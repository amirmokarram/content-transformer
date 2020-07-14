using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ContentTransformer.Common.ContentSource;

namespace ContentTransformer.Services.ContentSource.Sources
{
    [ContentSourceConfig(ArchiveNameConfig, Title = "The archive directory name", ConfigType = ContentSourceConfigType.String, IsRequired = true)]
    [ContentSourceConfig(PathConfig, Title = "Physical address path for watching content", ConfigType = ContentSourceConfigType.String, IsRequired = true)]
    [ContentSourceConfig(FilterConfig, Title = "Filtering content", ConfigType = ContentSourceConfigType.String)]
    internal class FileSystemContentSource : ContentSource
    {
        #region Config Constant
        internal const string ArchiveNameConfig = "archiveName";
        internal const string PathConfig = "path";
        internal const string FilterConfig = "filter";
        #endregion

        private readonly FileSystemWatcher _watcher;
        private string _archiveDirectoryName;
        
        public FileSystemContentSource()
        {
            _watcher = new FileSystemWatcher();
            _watcher.EnableRaisingEvents = false;
        }

        #region Implementation of IContentSource
        public override void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }
        public override void Pause()
        {
            _watcher.EnableRaisingEvents = false;
        }
        public override void Resume()
        {
            _watcher.EnableRaisingEvents = true;
        }
        public override byte[] Read(ContentSourceItem item)
        {
            return File.ReadAllBytes(item.Uri.AbsolutePath);
        }
        public override void Archive(ContentSourceItem item)
        {
            File.Move(item.Uri.AbsolutePath, Path.Combine(_archiveDirectoryName, Path.GetFileName(item.Uri.AbsolutePath)));
        }
        #endregion

        #region Overrides of ContentSource
        protected override void OnInit()
        {
            string archiveNameValue = ResolveParameter<string>(ArchiveNameConfig);
            string normalizeArchiveName = archiveNameValue.StartsWith("$") ? archiveNameValue : $"${archiveNameValue}";
            _archiveDirectoryName = Path.Combine(ResolveParameter<string>(PathConfig), normalizeArchiveName);

            if (!Directory.Exists(_archiveDirectoryName))
                Directory.CreateDirectory(_archiveDirectoryName);

            _watcher.BeginInit();
            _watcher.Path = ResolveParameter<string>(PathConfig);
            _watcher.Created += (sender, args) =>
            {
                if (args.ChangeType != WatcherChangeTypes.Created)
                    return;

                ContentSourceItem item = new ContentSourceItem(DateTime.Now, new Uri(args.FullPath));
                RaiseSourceChanged(item);
            };
            _watcher.EndInit();

            RaiseSourceChanged(ReadExistItems().ToArray());
        }

        protected override IEnumerable<ContentSourceItem> ReadExistItems()
        {
            string filterValue = ResolveParameter<string>(FilterConfig);
            List<ContentSourceItem> result = new List<ContentSourceItem>();
            foreach (string file in Directory.GetFiles(ResolveParameter<string>(PathConfig), filterValue ?? "*.*"))
                result.Add(new ContentSourceItem(File.GetCreationTime(file), new Uri(file)));
            return result;
        }
        protected override void Dispose(bool disposing)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();

            base.Dispose(disposing);
        }
        #endregion
    }
}