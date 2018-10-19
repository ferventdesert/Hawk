using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("JoinDBTF","JoinDBTF_desc" )]
    public class JoinDBTF : TransformerBase
    {
        private readonly IDataManager dataManager;

        public JoinDBTF()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["DataManager"] as IDataManager;
            ConnectorSelector = new ExtendSelector<IDataBaseConnector>();
            TableSelector = new ExtendSelector<TableInfo>();
            ImportColumns = new ObservableCollection<string>();
            ConnectorSelector.GetItems = () => dataManager.CurrentConnectors.ToList();
            ConnectorSelector.SelectChanged +=
                (s, e) => TableSelector.SetSource(ConnectorSelector.SelectItem.RefreshTableNames());
           
            TableSelector.SelectChanged += (s, e) =>
            {
                IDataBaseConnector connector = ConnectorSelector.SelectItem;
                if (connector == null)
                    return;
                TableInfo table = TableSelector.SelectItem;
                if (table == null)
                    return;
                IEnumerable<IDictionarySerializable> datas = ConnectorSelector.SelectItem.GetEntities(table.Name,
                   10, 0);
                IEnumerable<string> keys = datas.GetKeys();
                ImportColumns.Clear();
                foreach (string key in keys)
                {
                    ImportColumns.Add(key);
                }
            };
        }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            OneOutput = false;
            return base.Init(docus);
        }

        [LocalizedDisplayName("key_490")]
        [LocalizedDescription("key_491")]
        public bool IsMutliDatas { get; set; }

        [LocalizedDisplayName("key_492")]
        [LocalizedDescription("key_493")]
        public DBSearchStrategy SearchStrategy { get; set; }

        [LocalizedDisplayName("connector")]
        [LocalizedDescription("key_406")]
        [PropertyOrder(1)]
        public ExtendSelector<IDataBaseConnector> ConnectorSelector { get; set; }

        [LocalizedDisplayName("key_22")]
        [LocalizedDescription("key_495")]
        [PropertyOrder(1)]
        public ExtendSelector<TableInfo> TableSelector { get; set; }

        [LocalizedDisplayName("key_496")]
        [LocalizedDescription("key_493")]
        public string KeyName { get; set; }

      

        [LocalizedDisplayName("key_497")]
        [LocalizedDescription("key_498")]
        public ObservableCollection<string> ImportColumns { get; set; }

         

        public override object TransformData(IFreeDocument datas)
        {
            object item = datas[Column];
          
            IDataBaseConnector con = ConnectorSelector.SelectItem;
            if (con == null)
                return null;
            TableInfo table = TableSelector.SelectItem;
            if (table == null)
                return null;

            var keys = KeyName.Split(' ');
            var query = keys.ToDictionary(d => d, d => datas[d]); 
            if (IsMutliDatas)
            {
                var r = con.TryFindEntities(table.Name, query, null, -1,
                    SearchStrategy);
                if (r.Any() == false)
                    return null;
                var dicts = r.Select(d => d.DictSerialize()).ToList();
                foreach (string importColumn in ImportColumns)
                {
                    List<object> res = new List<object>();
                    for (int i = 0; i < dicts.Count; i++)
                    {

                        res.Add(dicts[i][importColumn]);
                    }
                    if(res.Count!=0)
                         datas.SetValue(importColumn, res);
                }
            
              
              
             
            }
            else
            {
                var r = con.TryFindEntities(table.Name, query,null,1,SearchStrategy).FirstOrDefault();
                if (r == null)
                    return null;
                FreeDocument dict = r.DictSerialize();
                foreach (string importColumn in ImportColumns)
                {
                    datas.SetValue(importColumn, dict[importColumn]);
                }
                
            }
           
            return null;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            FreeDocument dict = base.DictSerialize(scenario);
            dict.Add("Connector", ConnectorSelector.SelectItem?.Name);
            dict.Add("TableName", TableSelector.SelectItem?.Name);
            dict.Add("Columns", ImportColumns.Count==0? "":ImportColumns.Aggregate((a, b) => a + ' ' + b));
            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu);
            ConnectorSelector.SelectItem =
                dataManager.CurrentConnectors.FirstOrDefault(d => d.Name == docu["Connector"].ToString());
            TableSelector.InformPropertyChanged("");
            IDataBaseConnector connector = ConnectorSelector.SelectItem;
            if (connector == null)
                return;
            TableSelector.SelectItem =
                TableSelector.Collection.FirstOrDefault(d => d.Name == docu["TableName"].ToString());
            ImportColumns.Clear();
            foreach (string item in docu["Columns"].ToString().Split(' '))
            {
                ImportColumns.Add(item);
            }
        }
    }
}