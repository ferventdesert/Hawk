using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors.Vitural
{
    public class DataBaseVirtualProvider<T> : IItemsProvider<T> where T : class, IFreeDocument
    {
        public IDataBaseConnector Connector { get; private set; }
        private string tableName;
        public DataBaseVirtualProvider(IDataBaseConnector db, string mtableName)
        {
            Connector = db;
            tableName = mtableName;
        }

        private int? count = null;
        public int FetchCount()
        {
            if (count == null)
            {
                var table = Connector.RefreshTableNames().FirstOrDefault(d => d.Name == tableName);
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

        private List<T> buffer;
        public IList<T> FetchRange(int startIndex, int count)
        {

            if (startIndex == 0)
            {
                if (buffer == null)
                {
                    buffer = Connector.GetEntityList<T>(tableName, count, startIndex);
                    return buffer;
                }
                   
                if (buffer.Count < count && FetchCount() > count)
                {
                    
                }
                else
                {
                    return buffer;
                }
            }
            return Connector.GetEntityList<T>(tableName, count, startIndex);
        }

        public event EventHandler AlreadyGetSize;
    }
}
