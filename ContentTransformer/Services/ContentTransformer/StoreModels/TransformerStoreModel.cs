using System;
using ContentTransformer.Common.Services.ContentTransformer;

namespace ContentTransformer.Services.ContentTransformer.StoreModels
{
    internal class TransformerStoreModel : ITransformerStoreModel
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public string Name { get; set; }
        public string TransformerType { get; set; }
        public string SourceIdentity { get; set; }
    }
}