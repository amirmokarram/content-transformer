using System;

namespace ContentTransformer.Common.Services.ContentSource
{
    public class ContentSourceItem
    {
        public ContentSourceItem(DateTime dateTime, Uri uri)
        {
            DateTime = dateTime;
            Uri = uri;
        }
        
        public DateTime DateTime { get; }
        public Uri Uri { get; }

        public override bool Equals(object obj)
        {
            return obj is ContentSourceItem other && DateTime.Equals(other.DateTime) && Equals(Uri, other.Uri);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (DateTime.GetHashCode() * 397) ^ (Uri != null ? Uri.GetHashCode() : 0);
            }
        }
    }
}