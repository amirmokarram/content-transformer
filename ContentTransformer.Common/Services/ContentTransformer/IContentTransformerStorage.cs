using System;
using System.Collections.Generic;
using ContentTransformer.Common.Services.ContentSource;

namespace ContentTransformer.Common.Services.ContentTransformer
{
    public interface ITransformerStoreModel
    {
        int Id { get; }
        DateTime Created { get; }
        string TransformerType { get; }
        string SourceIdentity { get; }
    }
    public interface IContentStoreModel
    {
        int Id { get; }
        int TransformerId { get; }
        DateTime Created { get; }
        int ContentHash { get; }
        string ContentFileName { get; }
        byte[] Load();
    }
    public interface IContentTransformerStorage
    {
        IEnumerable<ITransformerStoreModel> GetTransformers();
        ITransformerStoreModel AddOrGetTransformer(IContentTransformer transformer, IContentSource source);
        void AddContent(IContentTransformer transformer, IContentSource source, byte[] content);
        IEnumerable<IContentStoreModel> GetContents(int transformerId);
    }
}