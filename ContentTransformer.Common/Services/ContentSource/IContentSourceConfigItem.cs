namespace ContentTransformer.Common.Services.ContentSource
{
    public interface IContentSourceConfigItem
    {
        string Name { get; }
        string Title { get; }
        bool IsRequired { get; }
        ContentSourceConfigType ConfigType { get; }
    }
}