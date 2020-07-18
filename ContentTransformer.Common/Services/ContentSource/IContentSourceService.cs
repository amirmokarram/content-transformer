namespace ContentTransformer.Common.Services.ContentSource
{
    public interface IContentSourceService
    {
        IContentSource Build(string name);
    }
}
