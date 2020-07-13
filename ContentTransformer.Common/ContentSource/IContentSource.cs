using System;
using System.Collections.Generic;

namespace ContentTransformer.Common.ContentSource
{
    public interface IContentSource : IDisposable
    {
        event EventHandler<ContentSourceEventArgs> SourceChanged;
        void Init(IDictionary<string, string> parameters);
        void Start();
        void Pause();
        void Resume();
        IEnumerable<ContentSourceItem> Items { get; }
    }
}