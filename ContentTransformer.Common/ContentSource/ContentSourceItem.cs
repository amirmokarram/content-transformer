using System;
using System.Security.Policy;

namespace ContentTransformer.Common.ContentSource
{
    public class ContentSourceItem
    {
        public ContentSourceItem(DateTime dateTime, Url url)
        {
            DateTime = dateTime;
            Url = url;
        }
        
        public DateTime DateTime { get; }
        public Url Url { get; }

        public override bool Equals(object obj)
        {
            return obj is ContentSourceItem other && DateTime.Equals(other.DateTime) && Equals(Url, other.Url);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (DateTime.GetHashCode() * 397) ^ (Url != null ? Url.GetHashCode() : 0);
            }
        }
    }
}