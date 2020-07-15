using System;
using System.Collections.Generic;

namespace ContentTransformer.Common.ContentSource
{
    public interface IContentSource : IDisposable
    {
        event EventHandler<ContentSourceEventArgs> SourceChanged;

        IEnumerable<IContentSourceConfigItem> ConfigItems { get; }

        void Init(IDictionary<string, string> parameters);
        void Start();
        void Pause();
        void Resume();
        byte[] Read(ContentSourceItem item);
        void Archive(ContentSourceItem item);
    }
}