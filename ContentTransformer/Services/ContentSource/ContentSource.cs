using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ContentTransformer.Common.ContentSource;

namespace ContentTransformer.Services.ContentSource
{
    internal abstract class ContentSource : IContentSource
    {
        private readonly Dictionary<string, IContentSourceConfigItem> _configItems;
        private readonly Dictionary<string, string> _parameterValues;

        protected ContentSource()
        {
            _configItems = GetType().GetCustomAttributes().OfType<ContentSourceConfigAttribute>().ToDictionary(k => k.Name.ToLower(), v => (IContentSourceConfigItem)v);
            _parameterValues = new Dictionary<string, string>();
        }

        #region Implementation of IContentSource
        public event EventHandler<ContentSourceEventArgs> SourceChanged;

        public IEnumerable<IContentSourceConfigItem> ConfigItems
        {
            get { return _configItems.Values; }
        }
        public void Init(IDictionary<string, string> parameters)
        {
            foreach (IContentSourceConfigItem configItem in ConfigItems.Where(x => x.IsRequired))
            {
                if (!parameters.ContainsKey(configItem.Name))
                    throw new Exception($"The config item '{configItem.Name}' is not present in parameters");
            }
            foreach (KeyValuePair<string, string> parameter in parameters)
                _parameterValues.Add(parameter.Key.ToLower(), parameter.Value);
            OnInit();
        }

        public abstract void Start();
        public abstract void Pause();
        public abstract void Resume();
        public abstract byte[] Read(ContentSourceItem item);
        public abstract void Archive(ContentSourceItem item);
        #endregion

        #region Implementation of IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        protected abstract void OnInit();
        protected abstract IEnumerable<ContentSourceItem> ReadExistItems();

        protected TValue ResolveParameter<TValue>(string parameterName)
            where TValue : class
        {
            if (!_parameterValues.TryGetValue(parameterName.ToLower(), out string parameterValue))
                return default;
            IContentSourceConfigItem configItem = _configItems[parameterName.ToLower()];
            switch (configItem.ConfigType)
            {
                case ContentSourceConfigType.String:
                    return parameterValue as TValue;
                case ContentSourceConfigType.Integer:
                    return Convert.ToInt32(parameterValue) as TValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void RaiseSourceChanged(params ContentSourceItem[] items)
        {
            if (items ==null || items.Length == 0)
                return;
            SourceChanged?.Invoke(this, new ContentSourceEventArgs(items));
        }
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}