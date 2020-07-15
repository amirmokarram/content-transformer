namespace ContentTransformer.Common.Services.ModuleDiscovery
{
    public interface IModuleInfo
    {
        string Name { get; }
        string AssemblyName { get; }
    }
}