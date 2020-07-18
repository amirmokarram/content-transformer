using System.Collections.Generic;
using System.IO;

namespace ContentTransformer.Common.Services.ContentTransformer
{
    public interface IContentTransformer
    {
        Stream Transform(IEnumerable<IContentStoreModel> contents);
    }
}
