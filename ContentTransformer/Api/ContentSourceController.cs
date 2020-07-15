using System.Web.Http;

namespace ContentTransformer.Api
{
    [RoutePrefix("content-source-api")]
    public class ContentSourceController : ApiController
    {
        [HttpGet]
        [Route("get")]
        public object Get()
        {
            return "test";
        }
    }
}