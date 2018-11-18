using System;
using System.Collections.Generic;
using System.Linq;
using Hawk.Core.Connectors.Vitural;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    public static class DbExtends
    {
        public static List<T> GetEntityList<T>(this IDataBaseConnector connector, string tableName, int mount = -1,
            int skip = 0)
            where T : class, IFreeDocument
        {
            var type = typeof (T);
            if (type.IsInterface)
                type = typeof (FreeDocument);
            var items = connector.GetEntities(tableName,  mount, skip).Select(d => d as T).ToList();
            return items;
        }

        public static List<T> TryFindEntities<T>(this IDataBaseConnector connector, string tableName,
            IDictionary<string, object> search) where T : class, IDictionarySerializable
        {
            var rs = connector.TryFindEntities(tableName, search );
            return rs.Select(d => d as T).ToList();
        }

        public static IItemsProvider<T> GetVirtualProvider<T>(this TableInfo tableInfo) where T : class, IFreeDocument
        {
            var enumable = tableInfo.Connector as IEnumerableProvider<T>;
            IItemsProvider<T> vir = null;
            if (enumable != null && enumable.CanSkip(tableInfo.Name) == false)
            {
                vir = new EnumableVirtualProvider<T>(
                    enumable.GetEnumerable(tableInfo.Name), tableInfo.Size);
            }
            else
            {
                vir = new DataBaseVirtualProvider<T>(tableInfo.Connector, tableInfo.Name);
            }
            return vir;
        }

        /// <summary>
        ///     获取数据库中的所有实体，通过传递实例化委托，提升反射性能
        /// </summary>
        /// <returns></returns>
        public static List<T> GetAllEntities<T>(this IDataBaseConnector connector, string tableName)
            where T : class, IFreeDocument, new()
        {
            return connector.GetEntityList<T>(tableName);
        }
    }

    public enum EntityExecuteType
    {
        OnlyInsert,
        InsertOrUpdate,
        Delete,
        OnlyUpdate
    }

    /// <summary>
    ///     基本数据库管理接口
    /// </summary>
    [Interface("IDataBaseConnector")]
    public interface IDataBaseConnector
    {
        /// <summary>
        ///     数据库类型
        /// </summary>
        string TypeName { get; }

        /// <summary>
        ///     连接名称
        /// </summary>
        string Name { get; set; }

        string ConnectionString { get; set; }

        /// <summary>
        ///     自动连接
        /// </summary>
        bool AutoConnect { get; set; }

        //数据库名 
        string DBName { get; set; }

        /// <summary>
        ///     是否可用
        ///     <remarks>数据库服务可能处于离线模式</remarks>
        /// </summary>
        bool IsUseable { get; }

        /// <summary>
        ///     保存数据到数据库
        /// </summary>
        /// <param name="source">要保存的数据</param>
        /// <param name="dbTableName">表名称</param>
        void BatchInsert(IEnumerable<IFreeDocument> source, List<string> keys, string dbTableName);

        /// <summary>
        ///     获取当前目录下的表名
        /// </summary>
        /// <returns></returns>
        List<TableInfo> RefreshTableNames();

        /// <summary>
        ///     搜索一个实体
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="search"></param>
        /// <param name="count">检索的数量</param>
        /// <param name="searchStrategy">搜索策略</param>
        /// <returns></returns>
        List<FreeDocument> TryFindEntities(string tableName, IDictionary<string, object> search,List<string> resultKeys=null,
            int count = -1, DBSearchStrategy searchStrategy = DBSearchStrategy.Contains);

        /// <summary>
        ///     创建一个数据库
        /// </summary>
        /// <param name="dbname"></param>
        void CreateDataBase(string dbname);

        /// <summary>
        ///     通过sql查询
        /// </summary>
        /// <param name="querySQL"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        List<FreeDocument> QueryEntities(string querySQL, out int count, string tableName = null);

        /// <summary>
        ///     连接数据库
        /// </summary>
        /// <returns></returns>
        bool ConnectDB();

        /// <summary>
        ///     关闭数据库
        /// </summary>
        /// <returns></returns>
        bool CloseDB();

        /// <summary>
        ///     获取对应表名的数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="skip">跳过 </param>
        /// <param name="type">数据类型</param>
        /// <param name="mount"> 数量</param>
        /// <returns></returns>
        IEnumerable<FreeDocument> GetEntities(string tableName, int mount = -1, int skip = 0);

        /// <summary>
        ///     创建表
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="createStr">创建字符串</param>
        bool CreateTable(IFreeDocument example, string name);

        /// <summary>
        ///     更新到数据库
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="updateItem"></param>
        /// <param name="executeType">对数据实体执行的操作</param>
        void SaveOrUpdateEntity(IFreeDocument updateItem, string tableName, IDictionary<string, object> keys,
            EntityExecuteType executeType = EntityExecuteType.InsertOrUpdate);

        /// <summary>
        ///     删除表数据
        /// </summary>
        /// <param name="tableName"></param>
        void DropTable(string tableName);
    }
}