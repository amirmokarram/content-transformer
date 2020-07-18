using System;
using ContentTransformer.Common.Services.ContentTransformer;

namespace ContentTransformer.Services.ContentTransformer.StoreModels
{
    internal class ContentStoreModel : IContentStoreModel
    {
        public int Id { get; set; }
        public int TransformerId { get; set; }
        public DateTime Created { get; set; }
        public string ContentHash { get; set; }
        public string ContentPath { get; set; }
        public byte[] Load()
        {
            return null;
        }
    }
}