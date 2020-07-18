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
        string ContentHash { get; }
        string ContentPath { get; }
        byte[] Load();
    }
    public interface IContentTransformerStorage
    {
        bool Exist(IContentTransformer transformer, IContentSource source);
        ITransformerStoreModel Get(IContentTransformer transformer, IContentSource source);
        void Add(IContentTransformer transformer, IContentSource source);
        IEnumerable<IContentStoreModel> GetContents(IContentTransformer transformer, IContentSource source);
    }
}