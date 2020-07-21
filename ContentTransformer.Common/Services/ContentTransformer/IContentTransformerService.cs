using System.Collections.Generic;
using System.Net.Http;

namespace ContentTransformer.Common.Services.ContentTransformer
{
    public interface IContentTransformerService
    {
        HttpResponseMessage Transform(int id);
    }
}
