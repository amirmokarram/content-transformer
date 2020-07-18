using System;
using System.Collections.Generic;
using System.IO;

namespace ContentTransformer.Common.Services.ContentSource
{
    public interface IContentSource : IDisposable
    {
        event EventHandler<ContentSourceEventArgs> SourceChanged;

        IEnumerable<IContentSourceConfigItem> ConfigItems { get; }

        string Identity { get; }
        void Init(IDictionary<string, string> parameters);
        void Start();
        void Pause();
        void Resume();
        byte[] Read(ContentSourceItem item);
        void Archive(ContentSourceItem item);
        void Output(string name, Stream stream);
    }
}