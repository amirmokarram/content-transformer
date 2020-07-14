using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ContentTransformer.Common.ContentSource;
using FluentFTP;

namespace ContentTransformer.Services.ContentSource.Sources
{
    [ContentSourceConfig(ArchiveNameConfig, Title = "The archive directory name", ConfigType = ContentSourceConfigType.String, IsRequired = true)]
    [ContentSourceConfig(HostConfig, Title = "Host address", ConfigType = ContentSourceConfigType.String, IsRequired = true)]
    [ContentSourceConfig(UsernameConfig, Title = "Username", ConfigType = ContentSourceConfigType.String)]
    [ContentSourceConfig(PasswordConfig, Title = "Password", ConfigType = ContentSourceConfigType.String)]
    [ContentSourceConfig(IntervalConfig, Title = "Interval in seconds for timeout watch FTP host", ConfigType = ContentSourceConfigType.Integer)]
    internal class FtpContentSource : ContentSource
    {
        #region Config Constant
        internal const string ArchiveNameConfig = "archiveName";
        internal const string HostConfig = "host";
        internal const string UsernameConfig = "username";
        internal const string PasswordConfig = "password";
        internal const string IntervalConfig = "interval";
        #endregion

        private readonly object _syncObj = new object();
        private readonly FtpClient _ftpClient;
        private readonly Task _ftpPollingTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _paused;
        private int _interval = 10;
        private string _archiveDirectoryName;

        public FtpContentSource()
        {
            _ftpClient = new FtpClient();
            _ftpClient.ValidateCertificate += FtpClientValidateCertificate;

            _cancellationTokenSource = new CancellationTokenSource();
            _ftpPollingTask = new Task(FtpWatcher, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
        }

        #region Implementation of IContentSource
        public override void Start()
        {
            if (_ftpPollingTask.Status == TaskStatus.Running)
                return;
            _ftpPollingTask.Start();
        }
        public override void Pause()
        {
            if (_paused)
                return;
            Monitor.Enter(_syncObj);
            _paused = true;
        }
        public override void Resume()
        {
            if (!_paused)
                return;
            _paused = false;
            Monitor.Exit(_syncObj);
        }
        public override byte[] Read(ContentSourceItem item)
        {
            throw new NotImplementedException();
        }
        public override void Archive(ContentSourceItem item)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Overrides of ContentSource
        protected override void OnInit()
        {
            FtpProfile profile = new FtpProfile();
            profile.Encoding = Encoding.UTF8;
            profile.Host = ResolveParameter<string>(HostConfig);
            
            string username = ResolveParameter<string>(UsernameConfig);
            string password = ResolveParameter<string>(PasswordConfig);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                profile.Credentials = new NetworkCredential(username, password);

            _ftpClient.Connect(profile);
            
            string archiveNameValue = ResolveParameter<string>(ArchiveNameConfig);
            _archiveDirectoryName = archiveNameValue.StartsWith("$") ? archiveNameValue : $"${archiveNameValue}";

            if (!_ftpClient.DirectoryExists(_archiveDirectoryName))
                _ftpClient.CreateDirectory(_archiveDirectoryName);
        }
        protected override IEnumerable<ContentSourceItem> ReadExistItems()
        {
            List<ContentSourceItem> items = new List<ContentSourceItem>();
            foreach (FtpListItem ftpListItem in _ftpClient.GetListing())
            {
                if (ftpListItem.Type != FtpFileSystemObjectType.File)
                    continue;
                Uri fileUri = new UriBuilder(Uri.UriSchemeFtp, _ftpClient.Host, _ftpClient.Port, ftpListItem.FullName).Uri;
                items.Add(new ContentSourceItem(ftpListItem.Created, fileUri));
            }
            return items;
        }
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _cancellationTokenSource.Cancel();

            if (_paused)
                Monitor.Exit(_syncObj);

            if (_ftpClient.IsConnected)
                _ftpClient.Disconnect();
            _ftpClient.Dispose();
        }
        #endregion

        private void FtpWatcher()
        {
            void ThrowIfCanceled()
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            }

            while (true)
            {
                ThrowIfCanceled();
                lock (_syncObj) {}
                ThrowIfCanceled();

                List<ContentSourceItem> items = new List<ContentSourceItem>(ReadExistItems() ?? Enumerable.Empty<ContentSourceItem>());

                ThrowIfCanceled();
                RaiseSourceChanged(items.ToArray());
                
                Thread.Sleep(_interval * 1000);
            }
        }
        private void FtpClientValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }
    }
}
