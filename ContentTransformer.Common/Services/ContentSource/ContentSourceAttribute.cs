using System;

namespace ContentTransformer.Common.Services.ContentSource
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ContentSourceAttribute : Attribute
    {
        public ContentSourceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Title { get; set; }
    }
}