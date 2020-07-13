using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ContentTransformer.Common.ContentSource;
using FluentFTP;

namespace ContentTransformer.Services.ContentSource.Sources
{
    [ContentSourceConfig("host", Title = "FTP host address", ConfigType = ContentSourceConfigType.String, IsRequired = true)]
    [ContentSourceConfig("username", Title = "Username", ConfigType = ContentSourceConfigType.String)]
    [ContentSourceConfig("password", Title = "Password", ConfigType = ContentSourceConfigType.String)]
    [ContentSourceConfig("interval", Title = "Interval in seconds for timeout watch FTP host", ConfigType = ContentSourceConfigType.Integer)]
    internal class FtpContentSource : IContentSource
    {
        private readonly object _syncObj = new object();
        private bool _paused;

        private readonly FtpClient _ftpClient;

        private readonly Task _ftpPollingTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        public FtpContentSource()
        {
            _ftpClient = new FtpClient();
            _ftpClient.ValidateCertificate += FtpClientValidateCertificate;

            _cancellationTokenSource = new CancellationTokenSource();
            _ftpPollingTask = new Task(FtpWatcher, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
        }

        #region Implementation of IContentSource
        public event EventHandler<ContentSourceEventArgs> SourceChanged;

        public void Init(IDictionary<string, string> parameters)
        {
            FtpProfile profile = new FtpProfile();
            profile.Encoding = Encoding.UTF8;
            profile.Host = parameters["host"];
            if (parameters.TryGetValue("username", out string username) && parameters.TryGetValue("password", out string password))
                profile.Credentials = new NetworkCredential(username, password);
            _ftpClient.Connect(profile);
        }
        public void Start()
        {
            if (_ftpPollingTask.Status == TaskStatus.Running)
                return;
            _ftpPollingTask.Start();
        }
        public void Pause()
        {
            if (_paused)
                return;
            Monitor.Enter(_syncObj);
            _paused = true;
        }
        public void Resume()
        {
            if (!_paused)
                return;
            _paused = false;
            Monitor.Exit(_syncObj);
        }
        public IEnumerable<ContentSourceItem> Items
        {
            get
            {
                return null;
            }
        }
        #endregion

        private void FtpWatcher()
        {
            while (true)
            {
                lock (_syncObj) {}

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    break;
                }
                FtpListItem[] ftpListItems = _ftpClient.GetListing();

                SourceChanged?.Invoke(this, new ContentSourceEventArgs(null));
            }
        }

        private void FtpClientValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }

        #region Implementation of IDisposable
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();

            if (_paused)
                Monitor.Exit(_syncObj);

            if (_ftpClient.IsConnected)
                _ftpClient.Disconnect();
            _ftpClient.Dispose();
        }
        #endregion
    }
}
