using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ContentTransformer.Common;
using ContentTransformer.Common.Services.ContentSource;
using ContentTransformer.Common.Services.ContentTransformer;
using Unity;

namespace ContentTransformer.Services.ContentTransformer
{
    [Service(ServiceType = typeof(IContentTransformerService))]
    internal class ContentTransformerService : IContentTransformerService, IServiceInitializer, IDisposable
    {
        private readonly IUnityContainer _container;
        private readonly IContentSourceService _contentSourceService;
        private readonly IContentTransformerStorage _contentTransformerStorage;
        private readonly List<TransformerProcessor> _transformerProcessors;

        public ContentTransformerService(IUnityContainer container, IContentSourceService contentSourceService, IContentTransformerStorage contentTransformerStorage)
        {
            _container = container;
            _contentSourceService = contentSourceService;
            _contentTransformerStorage = contentTransformerStorage;

            _transformerProcessors = new List<TransformerProcessor>();
        }

        #region Implementation of IServiceInitializer
        public void Init()
        {
            TransformerCatalog transformerCatalog = TransformerCatalog.TryLoad();
            foreach (TransformerConfig transformerConfig in transformerCatalog.Transformers)
            {
                Type transformerType = Type.GetType(transformerConfig.TypeName);

                if (transformerType == null)
                    throw new Exception($"The type '{transformerConfig.TypeName}' was not found.");
                if (!typeof(IContentTransformer).IsAssignableFrom(transformerType))
                    throw new Exception($"The type '{transformerType.FullName}' is not assignable to type '{typeof(IContentTransformer).FullName}'.");

                IContentTransformer transformer = (IContentTransformer)_container.Resolve(transformerType);
                if (transformerConfig.ContentSource == null || string.IsNullOrEmpty(transformerConfig.ContentSource.Name))
                    throw new Exception($"The transformer '{transformerType.Name}' does not have any content source.");
                IContentSource source = _contentSourceService.Build(transformerConfig.ContentSource.Name);
                source.Init(transformerConfig.ContentSource.Config);

                _contentTransformerStorage.Add(transformer, source);
                _transformerProcessors.Add(new TransformerProcessor(transformer, source));
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            foreach (TransformerProcessor processor in _transformerProcessors)
                processor.Dispose();
        }
        #endregion

        #region Inner Type
        private class TransformerProcessor : IDisposable
        {
            private readonly IContentTransformer _transformer;
            private readonly IContentSource _source;
            private readonly ConcurrentBag<ContentSourceItem> _bufferedItems;

            public TransformerProcessor(IContentTransformer transformer, IContentSource source)
            {
                _bufferedItems = new ConcurrentBag<ContentSourceItem>();

                _transformer = transformer;
                _source = source;
                _source.SourceChanged += SourceChanged;
                _source.Start();
            }

            private void SourceChanged(object sender, ContentSourceEventArgs e)
            {
                foreach (ContentSourceItem item in e.Items)
                {
                    _source.Archive(item);
                }
            }

            #region IDisposable
            public void Dispose()
            {
                _source.Pause();
                _source.SourceChanged -= SourceChanged;
                _source.Dispose();
            }
            #endregion
        }
        #endregion
    }
}
