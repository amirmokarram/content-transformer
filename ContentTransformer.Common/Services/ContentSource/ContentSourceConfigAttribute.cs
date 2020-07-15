using System;

namespace ContentTransformer.Common.ContentSource
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ContentSourceConfigAttribute : Attribute, IContentSourceConfigItem
    {
        public ContentSourceConfigAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Title { get; set; }
        public bool IsRequired { get; set; }
        public ContentSourceConfigType ConfigType { get; set; }
    }
}