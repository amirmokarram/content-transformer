using System;
using System.IO;
using ContentTransformer.Common.Services.ContentTransformer;

namespace ContentTransformer.Services.ContentTransformer.StoreModels
{
    internal class ContentStoreModel : IContentStoreModel
    {
        public int Id { get; set; }
        public int TransformerId { get; set; }
        public DateTime Created { get; set; }
        public int ContentHash { get; set; }
        public string ContentFileName { get; set; }
        public byte[] Load()
        {
            string filename = Path.Combine(Environment.CurrentDirectory, ContentTransformerStorage.StoreDirectoryName, TransformerId.ToString(), ContentFileName);
            return File.ReadAllBytes(filename);
        }
    }
}