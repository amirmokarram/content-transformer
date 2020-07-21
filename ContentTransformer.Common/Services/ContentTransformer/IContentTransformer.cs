using System.Collections.Generic;
using System.IO;

namespace ContentTransformer.Common.Services.ContentTransformer
{
    public class TransformInfo
    {
        public string MimeType { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public Stream TransformStream { get; set; }
    }

    public interface IContentTransformer
    {
        TransformInfo Transform(IEnumerable<IContentStoreModel> contents);
    }
}
