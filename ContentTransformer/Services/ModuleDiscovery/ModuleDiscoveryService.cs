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
            List<Type> controllers = new List<Type>();
            List<ServiceInfo> onDemandServices = new List<ServiceInfo>();

            foreach (ModuleInfo module in _moduleCatalog.Modules)
            {
                if (module.ModuleController != null)
                    controllers.Add(module.ModuleController);

                foreach (ServiceInfo service in module.Services)
                {
                    _container.RegisterSingleton(service.ServiceType, service.InstanceType);
                    if (service.OnDemand)
                        onDemandServices.Add(service);
                }
            }

            foreach (ServiceInfo service in onDemandServices)
                ((IServiceInitializer) _container.Resolve(service.ServiceType)).Init();

            foreach (Type controller in controllers)
                ((IModuleController) _container.Resolve(controller)).Init();
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
            private readonly List<ServiceInfo> _services;

            public ModuleInfo()
            {
                _services = new List<ServiceInfo>();
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
            public Assembly ModuleAssembly { get; private set; }
            #endregion

            public Type ModuleController { get; private set; }
            public IEnumerable<ServiceInfo> Services
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
                if (string.IsNullOrEmpty(rootDirectory))
                    throw new DirectoryNotFoundException("The root of the directory for module discovery is not found.");
                ModuleAssembly = Assembly.LoadFile(Path.Combine(rootDirectory, $"{AssemblyName}.dll"));
                Type[] moduleAllTypes = ModuleAssembly.GetTypes();

                foreach (Type type in moduleAllTypes)
                {
                    if (typeof(IModuleController).IsAssignableFrom(type))
                    {
                        ModuleController = type;
                        continue;
                    }

                    if (type.GetCustomAttribute<ServiceAttribute>() != null)
                        _services.Add(new ServiceInfo(type));
                }
            }
            #endregion
        }
        public class ServiceInfo
        {
            public ServiceInfo(Type targetType)
            {
                ServiceAttribute serviceAttribute = targetType.GetCustomAttribute<ServiceAttribute>();
                if (serviceAttribute == null)
                    throw new NotSupportedException($"The target type '{targetType.FullName}' is not marked with 'ServiceAttribute'.");
                ServiceType = serviceAttribute.ServiceType ?? targetType;
                InstanceType = targetType;
                OnDemand = typeof(IServiceInitializer).IsAssignableFrom(targetType);
            }

            public Type ServiceType { get; }
            public Type InstanceType { get; }
            public bool OnDemand { get; }
        }
        #endregion
    }
}
