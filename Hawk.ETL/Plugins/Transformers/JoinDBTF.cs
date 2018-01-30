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
    [XFrmWork("数据库匹配","用于完成与数据库的join操作和匹配，目前测试不完善" )]
    public class 
        JoinDBTF : TransformerBase
    {
        private readonly IDataManager dataManager;

        public JoinDBTF()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["数据管理"] as IDataManager;
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

        [LocalizedDisplayName("查询多数据")]
        [LocalizedDescription("启用该项时，会查询多个满足条件的项，同时将同一列保存为数组")]
        public bool IsMutliDatas { get; set; }

        [LocalizedDisplayName("匹配方式")]
        [LocalizedDescription("字符串匹配，如like,contains等，符合sql标准语法")]
        public DBSearchStrategy SearchStrategy { get; set; }

        [LocalizedDisplayName("连接器")]
        [LocalizedDescription("选择所要连接的数据库服务")]
        [PropertyOrder(1)]
        public ExtendSelector<IDataBaseConnector> ConnectorSelector { get; set; }

        [LocalizedDisplayName("表名")]
        [LocalizedDescription("选择所要连接的表")]
        [PropertyOrder(1)]
        public ExtendSelector<TableInfo> TableSelector { get; set; }

        [LocalizedDisplayName("表主键")]
        [LocalizedDescription("字符串匹配，如like,contains等，符合sql标准语法")]
        public string KeyName { get; set; }

      

        [LocalizedDisplayName("导入列")]
        [LocalizedDescription("join成功后倒入哪些列")]
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