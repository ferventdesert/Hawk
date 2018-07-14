using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors.Vitural
{
    public abstract class VirtualConnectorBase : DBConnectorBase
    {
        protected VirtualConnectorBase()
        {
            CurrentTables = new ObservableCollection<TableInfo>();


        }

        protected string openfile = "打开新文件";
        [LocalizedCategory("数据表列表")]
        [LocalizedDisplayName("曾打开的文件")]
        public ObservableCollection<TableInfo> CurrentTables { get; set; }

        public override List<TableInfo> RefreshTableNames()
        {
            var item = CurrentTables.ToList();
            TableNames.SetSource(item);
            return item;
        }

        public virtual TableInfo AddTable(string tableName, string desc = null, bool shouldDescOnly = false)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return null;
            }
            TableInfo fir = CurrentTables.FirstOrDefault(d => d.Name == tableName);

            if (fir != null)
            {
                CurrentTables.Remove(fir);

                CurrentTables.Insert(0, fir);
                return fir;
            }

            var table = new TableInfo(tableName, this) { Description = desc };
            CurrentTables.Insert(1, table);

            TableNames.SetSource(CurrentTables);
            return table;
        }

        public override bool ConnectDB()
        {
            IsUseable = true;
            return true;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu, scenario);
            var dict = docu as FreeDocument;
            if (dict != null && dict.Children != null)
            {
                foreach (FreeDocument item in dict.Children)
                {
                    var doc = new TableInfo();
                    doc.DictDeserialize(item.DictSerialize());
                    doc.Connector = this;
                    if(doc.Name!= openfile)
                        CurrentTables.Add(doc);
                }
            }
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            FreeDocument dict = base.DictSerialize(scenario);
            dict.Children = new List<FreeDocument>();
            dict.Children.AddRange(CurrentTables.Where(d=>d.Name!= openfile).Select(d => d.DictSerialize()));
            return dict;
        }

      

        public override void DropTable(string tableName)
        {
            if(tableName==openfile)
                return;
            TableInfo t = CurrentTables.FirstOrDefault(d => d.Name == tableName);
            CurrentTables.Remove(t);
        }

        public override bool CloseDB()
        {
            IsUseable = false;
            return true;
        }
    }
}