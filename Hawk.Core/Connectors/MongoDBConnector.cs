using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using MongoDB;
using MongoDB.Configuration;

namespace Hawk.Core.Connectors
{
    /// <summary>
    ///     Mongo数据库服务
    /// </summary>
    [XFrmWork("MongoDB",  "提供MongoDB交互的数据库服务", "")]
    public class MongoDBConnector : DBConnectorBase, IEnumerableProvider<IFreeDocument>
    {
        #region Constants and Fields

        public IMongoDatabase DB;

        protected Mongo Mongo;

        private Document update;

        #endregion

        #region Properties

        public MongoDBConnector()
        {
            AutoIndexName = "id";
        }


        /// <summary>
        ///     本地数据库位置
        /// </summary>
        [Browsable(false)]
        public string LocalDBLocation { get; set; }

        #endregion

        #region Public Methods

        [DisplayName("自增主键名称")]
        public string AutoIndexName { get; set; }

        [DisplayName("启用自增主键写入")]
        public bool AutoIndexEnabled { get; set; }

        public IEnumerable<IFreeDocument> GetEnumerable(string tableName, Type type)
        {
            if (type == null)
                type = typeof (FreeDocument);
            IEnumerable<Document> docuemts = DB.GetCollection<Document>(tableName).FindAll().Documents;
            foreach (Document docuemt in docuemts)
            {
                object data = Activator.CreateInstance(type);
                var suck = (IFreeDocument) data;
                suck.DictDeserialize(docuemt);
                yield return suck;
            }
        }

        public bool CanSkip(string tableName)
        {
            TableInfo firstOrDefault = TableNames.Collection.FirstOrDefault(d => d.Name == tableName);
            return firstOrDefault != null &&
                   firstOrDefault.ColumnInfos.FirstOrDefault(d => d.Name == AutoIndexName) != null;
        }

        public override void BatchInsert(IEnumerable<IFreeDocument> source, string dbTableName)
        {
            if (TableNames.Collection.FirstOrDefault(d => d.Name == dbTableName) == null)
            {
                CreateIndexTable(dbTableName);
                RefreshTableNames();
            }
            IMongoCollection<Document> collection = DB.GetCollection<Document>(dbTableName);
            if (collection == null) //需要重建
            {
            }

            int index = 0;
            //  public bool InsertEntity(IDictionarySerializable user, string tableName, string key, out int index)
            foreach (IFreeDocument item in source)
            {
                try
                {
                    InsertEntity(item, collection, dbTableName, index++);
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error("插入失败" + ex.Message);
                }
            }
        }

        protected override string GetConnectString()
        {
            //if (ConnectionString == null)
            //    return "mongodb://127.0.0.1";
            //if (ConnectionString.Contains("mongodb"))
            //    return ConnectionString;
            if (string.IsNullOrEmpty(UserName))
                return $"mongodb://{Server}";
            return $"mongodb://{Server}:{UserName}@{Password}";
        }

        protected override void ConnectStringToOtherInfos()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                Server = "127.0.0.1";
                return;
            }
               

            string str = ConnectionString.Replace("mongodb://", "");
            string[] items = str.Split(new[] {'@', ':', '/'});
            Server = items[0];
            if (items.Length > 1)
                UserName = items[1];
            if (items.Length > 2)
                Password = items[2];
        }

        /// <summary>
        ///     连接到数据库，只需执行一次
        /// </summary>
        public override bool ConnectDB()
        {
          ConnectionString = GetConnectString();
            var config = new MongoConfigurationBuilder();
            config.ConnectionString(ConnectionString);
            //定义Mongo服务 
            Mongo = new Mongo(config.BuildConfiguration());
            IsUseable = Mongo.TryConnect();
            if (IsUseable != true) return IsUseable;
            update = new Document {["$inc"] = new Document(AutoIndexName, 1)};
            DB = Mongo.GetDatabase(DBName);
            return IsUseable;
           

        }

        /// <summary>
        ///     创建一个自增主键索引表
        /// </summary>
        /// <param name="tableName">表名</param>
        public void CreateIndexTable(string tableName)
        {
            if (IsUseable == false)
            {
                return;
            }
            IMongoCollection idManager = DB.GetCollection("ids");
            Document idDoc = idManager.FindOne(new Document("Name", tableName));
            if (idDoc == null)
            {
                idDoc = new Document();
                idDoc["Name"] = tableName;
                idDoc[AutoIndexName] = 0;
            }
            else
            {
                idDoc[AutoIndexName] = 0;
            }

            idManager.Save(idDoc);
        }

        public override bool CreateTable(IFreeDocument example, string name)
        {
            CreateIndexTable(name);
            return true;
        }

        public override void DropTable(string tableName)
        {
            //MongoDB不支持数据库删除操作
            DB.Metadata.DropCollection(tableName);
        }


        /// <summary>
        ///     获取一定范围的实体
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<IDictionarySerializable> GetAllEntities(string tableName, Type type)
        {
            if (IsUseable == false)
            {
                return new List<IDictionarySerializable>();
            }

            List<Document> docuemts = DB.GetCollection<Document>(tableName).FindAll().Documents.ToList();
            var items = new List<IDictionarySerializable>();
            foreach (Document document in docuemts)
            {
                object data = Activator.CreateInstance(type);
                var suck = (IDictionarySerializable) data;
                suck.DictDeserialize(document);
                items.Add(suck);
            }
            return items;
        }

        public override List<IFreeDocument> QueryEntities(string querySQL, out int count, string tablename,
            Type type)
        {
            if (type == null)
                type = typeof (FreeDocument);
            List<Document> item = null;
            if (tablename == null)
            {
                item = DB.GetCollection<Document>().Find(querySQL).Documents.ToList();
            }
            else
            {
                item = DB.GetCollection<Document>(tablename).Find(querySQL).Documents.ToList();
            }
            count = item.Count;
            var items = new List<IFreeDocument>();
            foreach (Document document in item)
            {
                object data = Activator.CreateInstance(type);
                var suck = (IFreeDocument) data;
                suck.DictDeserialize(document);
                items.Add(suck);
            }
            return items;
        }

        /// <summary>
        ///     获取一定范围的实体
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="type"></param>
        /// <param name="mount"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        public override IEnumerable<IFreeDocument> GetEntities(
            string tableName, Type type, int mount = -1, int skip = 0)
        {
            if (IsUseable == false)
            {
                yield break;
            }
            if (TableNames == null)
            {
                IsUseable = false;
                yield break;
            }
              if (TableNames == null)
            {
                IsUseable = false;
                yield break;
            }
            TableInfo table = TableNames.Collection.FirstOrDefault(d => d.Name == tableName);
            if (table == null)
            {
                yield break;
            }

            ICursor<Document> collection = null;
            if (table.ColumnInfos != null && table.ColumnInfos.FirstOrDefault(d => d.Name == AutoIndexName) != null)
            {
                var query = new Document();

                query[AutoIndexName] = new Document("$gt", skip);


                collection = DB.GetCollection<Document>(tableName).Find(query).Sort(AutoIndexName, IndexOrder.Ascending);
            }
            else
            {
                collection = DB.GetCollection<Document>(tableName).FindAll();
                if (skip != 0)
                {
                    collection.Skip(skip);
                }
            }
            if (mount != -1)
            {
                collection = collection.Limit(mount);
            }


            foreach (Document document in collection.Documents)
            {
                IFreeDocument data = null;
                if (type == null)
                {
                    data = new FreeDocument();
                }
                else
                {
                    data = (IFreeDocument) Activator.CreateInstance(type);
                }


                data.DictDeserialize(document);
                yield return data;
            }
        }

        /// <summary>
        ///     直接插入一个实体
        /// </summary>
        /// <param name="user"></param>
        /// <param name="tableName"></param>
        private bool InsertEntity(IFreeDocument user, IMongoCollection<Document> collection, string tableName,
            int index = -1)
        {
            if (IsUseable == false)
            {
                return false;
            }
            Document id = null;
            if (AutoIndexEnabled)
            {
                IMongoCollection idManager = DB.GetCollection("ids");
                id = idManager.FindAndModify(update, new Document("Name", tableName), true);

                //下面三句存入数据库
            }

            Document doc = GetNewDocument(user);
            if (AutoIndexEnabled)
            {
                int v = (int) id[AutoIndexName] - 1;
                doc[AutoIndexName] = v;
            }
            else if (index >= 0)
            {
                doc[AutoIndexName] = index;
            }
            try
            {
                collection.Save(doc);
            }
            catch (Exception ex)
            {
              XLogSys.Print.Error("插入数据失败"+ex.Message);                
            }

          

            return true;
        }

        public override List<TableInfo> RefreshTableNames()
        {
            if (IsUseable == false || DB == null)
            {
                return new List<TableInfo>();
            }

            List<TableInfo> collectionNames = (from d in DB.GetCollectionNames()
                where d != null
                let m = d.Split('.')
                where m.Length == 2
                let index = DB.GetCollection(m[1]).Metadata.Indexes.Keys.ToList()
                let count = DB.GetCollection(m[1]).Count()
                select
                    new TableInfo(m[1], this)
                    {
                        Size = (int) count,
                        ColumnInfos = index.Select(name => new ColumnInfo(name)).ToList()
                    }).ToList();

            TableNames.SetSource(collectionNames);
            return collectionNames;
        }

        public void RepairDatabase()
        {
            bool local = (ConnectionString.Contains("localhost") || ConnectionString.Contains("127.0.0.1"));
            if (local == false)
            {
                throw new Exception("MongoDB数据库不在本地，无法启动自动数据库修复");
            }

            if (LocalDBLocation == null)
            {
                throw new Exception("数据库连接失败，请检查数据库配置，或手动设定MongoDB数据库的安装路径以方便自动修复");
            }
            var mydir = new DirectoryInfo(LocalDBLocation);
            FileInfo file = mydir.GetFiles().FirstOrDefault(d => d.Name == "mongod.lock");
            if (file == null)
            {
                throw new Exception("修复失败，您是否没有安装MongoDB数据库");
            }
            try
            {
                File.Delete(file.FullName);
                string str = CMDHelper.Execute("net start MongoDB");
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        ///     更新或增加一个新文档
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="tableName">表名 </param>
        /// <param name="keyName"> </param>
        /// <param name="keyvalue"> </param>
        public override void SaveOrUpdateEntity(
            IFreeDocument entity, string tableName, IDictionary<string, object> keys=null,
            EntityExecuteType executeType = EntityExecuteType.InsertOrUpdate)
        {
            if (IsUseable == false)
            {
                return;
            }
            IMongoCollection<Document> collection = DB.GetCollection<Document>(tableName);
            if (executeType == EntityExecuteType.OnlyInsert)
            {
                InsertEntity(entity, collection, tableName);
                return;
            }
            var query = new Document(keys);
            Document document = collection.FindOne(query);
            if (executeType == EntityExecuteType.Delete)
            {
                collection.Remove(query);
                return;
            }
            if (document != null)
            {
                UpdateDocument(entity, document);
                if (executeType == EntityExecuteType.InsertOrUpdate || executeType == EntityExecuteType.OnlyUpdate)

                {
                    collection.Save(document);
                }
            }
            else
            {
                if (executeType == EntityExecuteType.InsertOrUpdate || executeType == EntityExecuteType.OnlyInsert)
                    InsertEntity(entity, collection, tableName);
            }
        }

        public override List<IFreeDocument> TryFindEntities(string tableName,
            IDictionary<string, object> search
            , Type type = null, int count = -1, DBSearchStrategy searchStrategy = DBSearchStrategy.Contains)
        {
            if (type == null)
                type = typeof (FreeDocument);
            if(IsUseable==false)
                return new List<IFreeDocument>();
            IMongoCollection<Document> collection = DB.GetCollection<Document>(tableName);

            var querydoc = new Document();
            foreach (var r in search)
            {
                querydoc.Add(r.Key, r.Value);
            }
            if (count != 1)
            {
                ICursor<Document> document = collection.Find(querydoc);
                if (document == null)
                {
                    return new List<IFreeDocument>();
                }
                var results = new List<IFreeDocument>();
                
                foreach (Document item in document.Documents)
                {
                    if (count > 0)
                        count--;
                    if (count == 0)
                        break;
                    var result = Activator.CreateInstance(type) as IFreeDocument;

                    result.DictDeserialize(item);
                    results.Add(result);

                }


                return results;
            }
            else
            {
                 var  document = collection.FindOne(querydoc);
                if (document == null)
                {
                    return new List<IFreeDocument>();
                }
                var results = new List<IFreeDocument>();

              
                    var result = Activator.CreateInstance(type) as IFreeDocument;

                    result.DictDeserialize(document);
                    results.Add(result);
                return results;
            }
          
        }

        #endregion

        #region Methods

        public void UpdateDocument(IDictionarySerializable data, Document document)
        {
            IDictionary<string, object> datas = data.DictSerialize();
            foreach (string key in datas.Keys)
            {
                if (datas[key] is IDictionary<string, object>)
                {
                    document[key] = new Document(datas[key] as IDictionary<string, object>);
                }
                else if (datas[key] is List<IDictionary<string, object>>)
                {
                    var items = datas[key] as List<IDictionary<string, object>>;
                    document[key] = items.Select(d => new Document(d)).ToList();
                }
                else
                {
                    document[key] = datas[key];
                }
            }
        }


        private Document GetNewDocument(IDictionarySerializable entity)
        {
            IDictionary<string, object> datas = entity.DictSerialize();

            var document = new Document();
            foreach (string key in datas.Keys)
            {
                {
                    if (datas[key] is IDictionary<string, object>)
                    {
                        document[key] = new Document(datas[key] as IDictionary<string, object>);
                    }
                    else if (datas[key] is List<IDictionary<string, object>>)
                    {
                        var items = datas[key] as List<IDictionary<string, object>>;
                        document[key] = items.Select(d => new Document(d)).ToList();
                    }
                    else
                    {
                        document[key] = datas[key];
                    }
                }
            }

            return document;
        }

        #endregion

        //链接字符串 
    }
}