using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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
        private const string DatabaseName = "ContentTransformer.db";

        private readonly SQLiteConnection _dbConnection;
        private readonly List<TransformerStoreModel> _transformers;
        private readonly string _storeDirectoryName;

        public ContentTransformerStorage()
        {
            _storeDirectoryName = Path.Combine(Environment.CurrentDirectory, "$contents");
            if (!Directory.Exists(_storeDirectoryName))
                Directory.CreateDirectory(_storeDirectoryName);

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
        }

        #region Implementation of IContentTransformerStorage
        public bool Exist(IContentTransformer transformer, IContentSource source)
        {
            return _dbConnection.QuerySingleOrDefault<bool>($"SELECT 1 FROM Transformers WHERE TransformerType='{transformer.GetType().FullName}' AND SourceIdentity='{source.Identity}'");
        }
        public ITransformerStoreModel Get(IContentTransformer transformer, IContentSource source)
        {
            return _dbConnection.QueryFirst<TransformerStoreModel>($"SELECT * FROM Transformers WHERE TransformerType='{transformer.GetType().FullName}' AND SourceIdentity='{source.Identity}'");
        }
        public void Add(IContentTransformer transformer, IContentSource source)
        {
            _dbConnection.Execute($"INSERT INTO Transformers (Created, TransformerType, SourceIdentity) VALUES ('{DateTime.Now}', '{transformer.GetType().FullName}', '{source.Identity}')");
            _transformers.Add((TransformerStoreModel)Get(transformer, source));
        }
        public void Add()
        {
        }
        public IEnumerable<IContentStoreModel> GetContents(IContentTransformer transformer, IContentSource source)
        {
            TransformerStoreModel currentTransformer = _transformers.FirstOrDefault(x => x.TransformerType == transformer.GetType().FullName && x.SourceIdentity == source.Identity);
            return currentTransformer == null ? null : _dbConnection.Query<ContentStoreModel>($"SELECT * FROM Contents WHERE TransformerId={currentTransformer.Id}");
        }
        #endregion

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
