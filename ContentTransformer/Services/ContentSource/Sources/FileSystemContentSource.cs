using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using ContentTransformer.Common.ContentSource;

namespace ContentTransformer.Services.ContentSource.Sources
{
    [ContentSourceConfig("path", Title = "Physical address path for watching content", ConfigType = ContentSourceConfigType.String, IsRequired = true)]
    [ContentSourceConfig("filter", Title = "Filtering content", ConfigType = ContentSourceConfigType.String)]
    internal class FileSystemContentSource : IContentSource
    {
        private readonly FileSystemWatcher _watcher;
        private readonly Dictionary<string, string> _parameters;

        public FileSystemContentSource()
        {
            _watcher = new FileSystemWatcher();
            _watcher.EnableRaisingEvents = false;

            _parameters = new Dictionary<string, string>();
        }

        #region Implementation of IContentSource
        public event EventHandler<ContentSourceEventArgs> SourceChanged;

        public void Init(IDictionary<string, string> parameters)
        {
            if (parameters != null)
                foreach (KeyValuePair<string, string> parameterPair in parameters)
                    _parameters.Add(parameterPair.Key, parameterPair.Value);

            _watcher.BeginInit();
            _watcher.Path = _parameters["path"];
            _watcher.Created += (sender, args) =>
            {
                if (args.ChangeType != WatcherChangeTypes.Created)
                    return;

                ContentSourceItem newItem = new ContentSourceItem(DateTime.Now, new Url(args.FullPath));
                SourceChanged?.Invoke(this, new ContentSourceEventArgs(new[] { newItem }));
            };
            _watcher.EndInit();
        }
        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }
        public void Pause()
        {
            _watcher.EnableRaisingEvents = false;
        }
        public void Resume()
        {
            _watcher.EnableRaisingEvents = true;
        }
        public IEnumerable<ContentSourceItem> Items
        {
            get
            {
                _parameters.TryGetValue("filter", out string filter);
                List<ContentSourceItem> result = new List<ContentSourceItem>();
                foreach (string file in Directory.GetFiles(_parameters["path"], filter ?? "*.*", SearchOption.AllDirectories))
                    result.Add(new ContentSourceItem(File.GetCreationTime(file), new Url(file)));
                return result;
            }
        }
        #endregion

        #region Implementation of IDisposable
        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
        #endregion
    }
}