using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ContentTransformer.Common;
using ContentTransformer.Common.Services.ContentSource;
using Unity;

namespace ContentTransformer.Services.ContentSource
{
    [Service(ServiceType = typeof(IContentSourceService))]
    internal class ContentSourceService : IContentSourceService
    {
        private readonly IUnityContainer _container;
        private readonly Dictionary<string, Type> _registeredContentSources;

        public ContentSourceService(IUnityContainer container)
        {
            _container = container;
            _registeredContentSources = new Dictionary<string, Type>();

            IEnumerable<Type> types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => typeof(ContentSource).IsAssignableFrom(x) && x.GetCustomAttribute<ContentSourceAttribute>() != null);
            foreach (Type type in types)
            {
                ContentSourceAttribute contentSourceAttribute = type.GetCustomAttribute<ContentSourceAttribute>();
                _registeredContentSources.Add(contentSourceAttribute.Name.ToLower(), type);
            }
        }

        #region Implementation of IContentSourceService
        public IContentSource Build(string name)
        {
            if (!_registeredContentSources.TryGetValue(name.ToLower(), out Type contentSourceType))
                throw new Exception($"The name '{name}' is not registered.");
            return (IContentSource)_container.Resolve(contentSourceType);
        }
        #endregion
    }
}
