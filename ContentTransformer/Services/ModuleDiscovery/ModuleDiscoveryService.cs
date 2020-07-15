using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ContentTransformer.Common;
using ContentTransformer.Common.Services.ModuleDiscovery;
using Newtonsoft.Json;
using Unity;

namespace ContentTransformer.Services.ModuleDiscovery
{
    internal class ModuleDiscoveryService : IModuleDiscoveryService
    {
        private readonly IUnityContainer _container;
        private readonly ModuleCatalog _moduleCatalog;

        public ModuleDiscoveryService(IUnityContainer container)
        {
            _container = container;

            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(directoryName))
                throw new DirectoryNotFoundException("Directory module catalog is not found.");
            string catalogFileName = Path.Combine(directoryName, "ModuleCatalog.json");
            string catalogData = File.ReadAllText(catalogFileName);
            _moduleCatalog = JsonConvert.DeserializeObject<ModuleCatalog>(catalogData);
        }

        #region Implementation of IModuleDiscoveryService
        public IEnumerable<IModuleInfo> Modules
        {
            get
            {
                return _moduleCatalog.Modules;
            }
        }
        public void DiscoverModules()
        {
            foreach (ModuleInfo module in _moduleCatalog.Modules)
            {
                if (module.ModuleController != null)
                    ((IModuleController)_container.Resolve(module.ModuleController)).Init();
            }
        }
        #endregion
        
        #region Inner Type
        private class ModuleCatalog
        {
            public ModuleCatalog()
            {
                Modules = new List<ModuleInfo>();
            }

            [JsonProperty("modules")]
            public IList<ModuleInfo> Modules { get; }
        }
        private class ModuleInfo : IModuleInfo
        {
            private string _assemblyName;
            private readonly List<Type> _services;

            public ModuleInfo()
            {
                _services = new List<Type>();
            }

            #region Implementation of IModuleInfo
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("assembly")]
            public string AssemblyName
            {
                get { return _assemblyName; }
                set
                {
                    _assemblyName = value;
                    EnsureInit();
                }
            }
            #endregion

            public Type ModuleController { get; private set; }
            public IEnumerable<Type> Services
            {
                get { return _services; }
            }

            #region Load Module
            private bool _init;

            private void EnsureInit()
            {
                if (_init)
                    return;
                _init = true;

                string rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Assembly moduleAssembly = Assembly.LoadFile(Path.Combine(rootDirectory, $"{AssemblyName}.dll"));
                Type[] moduleAllTypes = moduleAssembly.GetTypes();

                foreach (Type type in moduleAllTypes)
                {
                    if (typeof(IModuleController).IsAssignableFrom(type))
                    {
                        ModuleController = type;
                        continue;
                    }

                    if (type.GetCustomAttribute<ServiceAttribute>() != null)
                    {
                        _services.Add(type);
                        continue;
                    }
                }
            }
            #endregion
        }
        #endregion
    }
}
