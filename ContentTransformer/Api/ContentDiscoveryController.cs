using System.Web.Http;

namespace ContentTransformer.Api
{
    [RoutePrefix("content-discovery-api")]
    public class ContentDiscoveryController : ApiController
    {
        [HttpGet]
        [Route("get")]
        public object Get()
        {
            return "test";
        }
    }
}