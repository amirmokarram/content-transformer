using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly FileSystemWatcher _watcher;
        
        public FileSystemContentSource()
        {
            _watcher = new FileSystemWatcher();
            _watcher.NotifyFilter = NotifyFilters.FileName;
            _watcher.EnableRaisingEvents = false;
            _watcher.Created += (sender, args) =>
            {
                if (args.ChangeType != WatcherChangeTypes.Created)
                    return;

                ContentSourceItem item = new ContentSourceItem(DateTime.Now, new Uri(args.FullPath));
                RaiseSourceChanged(item);
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
            File.Move(item.Uri.AbsolutePath, Path.Combine(ArchiveDirectoryName, Path.GetFileName(item.Uri.AbsolutePath)));
        }
        public override void Output(string name, Stream input)
        {
            string outputFileName = Path.Combine(OutputDirectoryName, name);
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

            _watcher.Path = ResolveParameter<string>(PathConfig);
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