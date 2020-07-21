using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using ContentTransformer.Common.Services.ContentSource;

namespace ContentTransformer.Services.ContentSource.Sources
{
    [ContentSource("FileSystem", Title = "File System")]
    [ContentSourceConfig(PathConfig, Title = "Physical address path for watching content", ConfigType = ContentSourceConfigType.String, IsRequired = true)]
    [ContentSourceConfig(FilterConfig, Title = "Filtering content", ConfigType = ContentSourceConfigType.String)]
    internal class FileSystemContentSource : ContentSource
    {
        #region Config Constant
        internal const string PathConfig = "path";
        internal const string FilterConfig = "filter";
        #endregion

        private readonly MemoryCache _memoryCache;
        private readonly CacheItemPolicy _cacheItemPolicy;
        private FileSystemWatcher _watcher;

        public FileSystemContentSource()
        {
            _memoryCache = MemoryCache.Default;
            _cacheItemPolicy = new CacheItemPolicy
            {
                RemovedCallback = OnRemovedFromCache
            };
        }

        #region Implementation of ContentSource
        public override string Identity
        {
            get
            {
                return $"FileSystem|{ResolveParameter<string>(PathConfig)}";
            }
        }
        public override void Start()
        {
            _watcher.EnableRaisingEvents = true;
            RaiseSourceChanged(ReadExistItems().ToArray());
        }
        public override void Pause()
        {
            _watcher.EnableRaisingEvents = false;
        }
        public override void Resume()
        {
            _watcher.EnableRaisingEvents = true;
            RaiseSourceChanged(ReadExistItems().ToArray());
        }
        public override byte[] Read(ContentSourceItem item)
        {
            return File.ReadAllBytes(item.Uri.AbsolutePath);
        }
        public override void Archive(ContentSourceItem item)
        {
            string archiveDirectoryName = Path.Combine(ResolveParameter<string>(PathConfig), ArchiveDirectoryName);
            string fileName = Path.GetFileNameWithoutExtension(item.Uri.AbsolutePath);
            string fileExtension = Path.GetExtension(item.Uri.AbsolutePath);

            string targetFileName = Path.Combine(archiveDirectoryName, $"{fileName}{fileExtension}");
            if (File.Exists(targetFileName))
            {
                int totalSameFiles = Directory.GetFiles(archiveDirectoryName, $"{fileName}*.*").Length;
                targetFileName = Path.Combine(ResolveParameter<string>(PathConfig), ArchiveDirectoryName, $"{fileName}_({totalSameFiles}){fileExtension}");
            }
            File.Move(item.Uri.AbsolutePath, targetFileName);
        }
        public override void Output(string name, Stream input)
        {
            string outputFileName = Path.Combine(ResolveParameter<string>(PathConfig), OutputDirectoryName, name);
            using (FileStream fileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
                input.CopyTo(fileStream);
        }
        #endregion

        #region Overrides of ContentSource
        protected override void OnInit()
        {
            string archivePath = Path.Combine(ResolveParameter<string>(PathConfig), ArchiveDirectoryName);
            if (!Directory.Exists(archivePath))
                Directory.CreateDirectory(archivePath);

            string outputPath = Path.Combine(ResolveParameter<string>(PathConfig), OutputDirectoryName);
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            InitWatcher();
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
            RemoveWatcher();
            _memoryCache.Dispose();
            base.Dispose(disposing);
        }
        #endregion

        private void OnRemovedFromCache(CacheEntryRemovedArguments args)
        {
            if (args.RemovedReason != CacheEntryRemovedReason.Expired)
                return;

            FileSystemEventArgs fileInfo = (FileSystemEventArgs)args.CacheItem.Value;

            if (!IsFileLocked(fileInfo.FullPath))
            {
                ContentSourceItem item = new ContentSourceItem(DateTime.Now, new Uri(fileInfo.FullPath));
                RaiseSourceChanged(item);
                return;
            }

            _cacheItemPolicy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(1000);
            _memoryCache.Add(fileInfo.Name, fileInfo, _cacheItemPolicy);
        }

        private void WatcherCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
                return;

            _cacheItemPolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(50);
            _memoryCache.AddOrGetExisting(e.Name, e, _cacheItemPolicy);

        }
        private void WatcherError(object sender, ErrorEventArgs e)
        {
            RemoveWatcher();
        }
        private void InitWatcher()
        {
            _watcher = new FileSystemWatcher();
            _watcher.NotifyFilter = NotifyFilters.FileName;
            _watcher.Created += WatcherCreated;
            _watcher.Error += WatcherError;
            _watcher.Path = ResolveParameter<string>(PathConfig);
            _watcher.WaitForChanged(WatcherChangeTypes.Created, 1000);
        }
        private void RemoveWatcher()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= WatcherCreated;
            _watcher.Error -= WatcherError;
            _watcher.Dispose();
        }

        private static bool IsFileLocked(string filePath)
        {
            FileStream stream = null;
            FileInfo file = new FileInfo(filePath);
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }
            return false;
        }
    }
}