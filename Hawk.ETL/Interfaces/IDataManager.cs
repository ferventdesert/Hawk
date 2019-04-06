using System;
using System.Collections.Generic;
using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Interfaces
{
    /// <summary>
    ///     数据管理接口
    /// </summary>
    public interface IDataManager : INotifyPropertyChanged
    {
        #region Properties

        IEnumerable<string> DataNameCollection { get; }

        /// <summary>
        ///     经过筛选等操作，从全部数据集合中获取的部分数据集合
        /// </summary>
        ICollection<DataCollection> DataCollections { get; }

        ICollection<IDataBaseConnector> CurrentConnectors { get; }

        event EventHandler DataSourceChanged;

        #endregion

        #region Public Methods

        DataCollection AddDataCollection(IEnumerable<IFreeDocument> source, string collectionName = null,
            bool isCover = false);

        void AddDataCollection(DataCollection collection);

        IList<IFreeDocument> Get(string name);
        void LoadDataConnections(ICollection<IDataBaseConnector> connectors);
        DataCollection GetSelectedCollection(string name);
        DataCollection ReadFile(string fileName, string fomrat = null);
        DataCollection ReadCollection(IDataBaseConnector connector, string tableName, bool isVirtual);
        #endregion

        void SaveFile(string dataCollectionName, string path = null, string format = null);
        void SaveFile(DataCollection dataCollection, string path=null, string format=null);
     
    }
}