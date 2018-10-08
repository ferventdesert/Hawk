using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors.Vitural
{
    public class DataBaseVirtualCollection : IItemsProvider<IFreeDocument>
    {
        private IDataBaseConnector connector;
        private string tableName;
        public DataBaseVirtualCollection(IDataBaseConnector db, string mtableName)
        {
            connector = db;
            tableName = mtableName;
        }

        private int? count = null;
        public int FetchCount()
        {
            if (count == null)
            {
                var table = connector.RefreshTableNames().FirstOrDefault(d => d.Name == tableName);
                if (table != null) count= table.Size;
                return count.Value;
            }
            else
            {
                return  count.Value;
            }
        }

        public string Name 
        {
            get { return GlobalHelper.Get("key_90"); }
        }

        public IList<IFreeDocument> FetchRange(int startIndex, int count)
        {
            return connector.GetEntityList<IFreeDocument>(tableName,  count, startIndex);
        }

        public event EventHandler AlreadyGetSize;
    }
}
