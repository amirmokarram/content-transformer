using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly Dictionary<int, TransformerProcessor> _transformerProcessors;

        public ContentTransformerService(IUnityContainer container, IContentSourceService contentSourceService, IContentTransformerStorage contentTransformerStorage)
        {
            _container = container;
            _contentSourceService = contentSourceService;
            _contentTransformerStorage = contentTransformerStorage;

            _transformerProcessors = new Dictionary<int, TransformerProcessor>();
        }

        #region Implementation of IContentTransformerService
        public HttpResponseMessage Transform(int id)
        {
            if (!_transformerProcessors.TryGetValue(id, out TransformerProcessor processor))
                throw new Exception($"The transformer with id '{id}' is not registered.");

            IEnumerable<IContentStoreModel> contentStoreModels = _contentTransformerStorage.GetContents(id);
            TransformInfo transformInfo = processor.Transform(contentStoreModels);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new StreamContent(transformInfo.TransformStream);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(transformInfo.MimeType);
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = $"{transformInfo.Name}{transformInfo.Extension}"
            };
            return result;
        }
        #endregion

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

                ITransformerStoreModel transformerStoreModel = _contentTransformerStorage.AddOrGetTransformer(transformer, source);
                _transformerProcessors.Add(transformerStoreModel.Id, new TransformerProcessor(_contentTransformerStorage, transformer, source));
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            foreach (TransformerProcessor processor in _transformerProcessors.Values)
                processor.Dispose();
            _transformerProcessors.Clear();
        }
        #endregion

        #region Inner Type
        private class TransformerProcessor : IDisposable
        {
            private readonly IContentTransformerStorage _storage;
            private readonly IContentTransformer _transformer;
            private readonly IContentSource _source;

            public TransformerProcessor(IContentTransformerStorage storage, IContentTransformer transformer, IContentSource source)
            {
                _storage = storage;
                _transformer = transformer;
                _source = source;
                _source.SourceChanged += SourceChanged;
                _source.Start();
            }

            public TransformInfo Transform(IEnumerable<IContentStoreModel> contents)
            {
                TransformInfo result = _transformer.Transform(contents);
                _source.Output($"{result.Name}{result.Extension}", result.TransformStream);
                result.TransformStream.Position = 0;
                return result;
            }

            private void SourceChanged(object sender, ContentSourceEventArgs e)
            {
                foreach (ContentSourceItem item in e.Items)
                {
                    _storage.AddContent(_transformer, _source, _source.Read(item));
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
