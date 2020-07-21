using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ContentTransformer.Common;
using ContentTransformer.Common.Services.ContentSource;
using ContentTransformer.Common.Services.ContentTransformer;
using ContentTransformer.Services.ContentTransformer.StoreModels;
using Dapper;

namespace ContentTransformer.Services.ContentTransformer
{
    [Service(ServiceType = typeof(IContentTransformerStorage))]
    internal class ContentTransformerStorage : IContentTransformerStorage, IDisposable
    {
        internal const string StoreDirectoryName = "$contents";
        private const string DatabaseName = "ContentTransformer.db";

        private readonly SQLiteConnection _dbConnection;
        private readonly List<TransformerStoreModel> _transformers;
        private readonly string _storeContentDirectoryName;

        public ContentTransformerStorage()
        {
            _storeContentDirectoryName = Path.Combine(Environment.CurrentDirectory, StoreDirectoryName);
            if (!Directory.Exists(_storeContentDirectoryName))
                Directory.CreateDirectory(_storeContentDirectoryName);

            _transformers = new List<TransformerStoreModel>();

            string databaseFileName = Path.Combine(Environment.CurrentDirectory, DatabaseName);
            bool isFirstTime = false;
            if (!File.Exists(databaseFileName))
            {
                SQLiteConnection.CreateFile(databaseFileName);
                isFirstTime = true;
            }

            _dbConnection = new SQLiteConnection($"Data Source={DatabaseName};Version=3;");
            
            if (!isFirstTime)
            {
                EnsureLoadTransformers();
                return;
            }
            
            _dbConnection.Execute(Properties.Resources.ContentTransformerDatabaseScript);
            EnsureLoadTransformers();
        }

        #region Implementation of IContentTransformerStorage
        public IEnumerable<ITransformerStoreModel> GetTransformers()
        {
            return _transformers;
        }
        public ITransformerStoreModel AddOrGetTransformer(IContentTransformer transformer, IContentSource source)
        {
            ITransformerStoreModel currentTransformer = _transformers.Find(model => model.TransformerType == transformer.GetType().FullName && model.SourceIdentity == source.Identity);
            if (currentTransformer != null)
                return currentTransformer;
            
            _dbConnection.Execute($"INSERT INTO Transformers (Created, Name, TransformerType, SourceIdentity) VALUES ('{DateTime.Now:yyyy-MM-dd hh:mm:ss}', '{transformer.GetType().Assembly.GetName().Name}', '{transformer.GetType().FullName}', '{source.Identity}')");

            TransformerStoreModel transformerStoreModel = (TransformerStoreModel)GetTransformer(transformer, source);
            Directory.CreateDirectory(Path.Combine(_storeContentDirectoryName, transformerStoreModel.Id.ToString()));
            _transformers.Add(transformerStoreModel);
            return transformerStoreModel;
        }
        public void AddContent(IContentTransformer transformer, IContentSource source, byte[] content)
        {
            ITransformerStoreModel currentTransformer = GetTransformer(transformer, source);

            string contentFileName = $"{Guid.NewGuid():N}.bin";
            string contentFilePath = Path.Combine(_storeContentDirectoryName, currentTransformer.Id.ToString(), contentFileName);
            int contentHash = BitConverter.ToInt32(new MD5CryptoServiceProvider().ComputeHash(content), 0);

            try
            {
                lock (_dbConnection)
                {
                    if (ExistContent(currentTransformer.Id, contentHash))
                        return;

                    _dbConnection.Execute($"INSERT INTO Contents (TransformerId, Created, ContentHash, ContentFileName) VALUES ({currentTransformer.Id}, '{DateTime.Now:yyyy-MM-dd hh:mm:ss}', '{contentHash}', '{contentFileName}')");
                    
                    File.WriteAllBytes(contentFilePath, content);
                }
            }
            catch
            {
                if (File.Exists(contentFilePath))
                    File.Delete(contentFilePath);
            }
        }
        public IEnumerable<IContentStoreModel> GetContents(int transformerId)
        {
            return _dbConnection.Query<ContentStoreModel>($"SELECT * FROM Contents WHERE TransformerId={transformerId}");
        }
        #endregion

        private bool ExistTransformer(IContentTransformer transformer, IContentSource source)
        {
            return _dbConnection.QuerySingleOrDefault<bool>($"SELECT 1 FROM Transformers WHERE TransformerType='{transformer.GetType().FullName}' AND SourceIdentity='{source.Identity}'");
        }
        private bool ExistContent(int transformerId, int contentHash)
        {
            return _dbConnection.QuerySingleOrDefault<bool>($"SELECT 1 FROM Contents WHERE TransformerId={transformerId} AND ContentHash={contentHash}");
        }
        private ITransformerStoreModel GetTransformer(IContentTransformer transformer, IContentSource source)
        {
            TransformerStoreModel currentTransformer = _transformers.FirstOrDefault(x => x.TransformerType == transformer.GetType().FullName && x.SourceIdentity == source.Identity);
            return currentTransformer ?? _dbConnection.QueryFirst<TransformerStoreModel>($"SELECT * FROM Transformers WHERE TransformerType='{transformer.GetType().FullName}' AND SourceIdentity='{source.Identity}'");
        }
        private void EnsureLoadTransformers()
        {
            _transformers.AddRange(_dbConnection.Query<TransformerStoreModel>("SELECT * FROM Transformers"));
        }

        #region IDisposable
        public void Dispose()
        {
            _dbConnection?.Close();
            _dbConnection?.Dispose();
        }
        #endregion
    }
}
