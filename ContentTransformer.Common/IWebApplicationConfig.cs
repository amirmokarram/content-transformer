namespace ContentTransformer.Common
{
    public interface IWebApplicationConfig
    {
        bool UseSSL { get; }
        bool IsLocalMode { get; }
        int Port { get; }
        string GetUrl();
    }
}