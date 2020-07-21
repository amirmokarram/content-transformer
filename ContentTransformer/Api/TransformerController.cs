using System.Web.Http;
using ContentTransformer.Common.Services.ContentTransformer;

namespace ContentTransformer.Api
{
    [RoutePrefix("transformer-api")]
    public class TransformerController : ApiController
    {
        private readonly IContentTransformerService _transformerService;
        private readonly IContentTransformerStorage _storage;

        public TransformerController(IContentTransformerService transformerService, IContentTransformerStorage storage)
        {
            _transformerService = transformerService;
            _storage = storage;
        }

        [HttpGet]
        [Route("transforms")]
        public object Transforms()
        {
            return _storage.GetTransformers();
        }

        [HttpGet]
        [Route("transform/{id}")]
        public object Transform([FromUri]int id)
        {
            return _transformerService.Transform(id);
        }
    }
}