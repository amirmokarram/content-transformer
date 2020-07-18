using System;
using System.Collections.Generic;

namespace ContentTransformer.Common.Services.ContentSource
{
    public class ContentSourceEventArgs : EventArgs
    {
        public ContentSourceEventArgs(IEnumerable<ContentSourceItem> items)
        {
            Items = items;
        }

        public IEnumerable<ContentSourceItem> Items { get; }
    }
}