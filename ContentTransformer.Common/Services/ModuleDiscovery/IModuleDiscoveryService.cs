using System.Collections.Generic;

namespace ContentTransformer.Common.Services.ModuleDiscovery
{
    public interface IModuleDiscoveryService
    {
        IEnumerable<IModuleInfo> Modules { get; }
        void DiscoverModules();
    }
}
