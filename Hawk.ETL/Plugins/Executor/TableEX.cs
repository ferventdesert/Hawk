using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("写入数据表","将结果保存至软件的数据管理器中，之后可方便进行其他处理")]
    public class TableEX : DataExecutorBase
    {
        private readonly IDataManager dataManager;

        public TableEX()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["数据管理"] as IDataManager;
            TableSelector = new ExtendSelector<DataCollection>();
            TableSelector.GetItems = () => dataManager.DataCollections.ToList();
        }

        [DisplayName("数据表")]
        [Description("选择所要连接的数据表")]
        [PropertyOrder(1)]
        public ExtendSelector<DataCollection> TableSelector { get; set; }


        [DisplayName("新建表名")]
        [Description("如果要新建表，则填写此项，否则留空")]
        public string NewTableName { get; set; }

        private DataCollection collection;

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
             collection = dataManager.DataCollections.FirstOrDefault(d => d.Name == NewTableName);
            if (collection == null)

            {
                collection = new DataCollection(new List<IDictionarySerializable>()) { Name = NewTableName };
                dataManager.AddDataCollection(collection);
            }

            return base.Init(datas);
        }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
          

            foreach (IFreeDocument computeable in documents)
            {
                collection.ComputeData.Add(computeable);
                collection.OnPropertyChanged("Count");
                yield return computeable;
            }
        
              
           
          
        }

    
        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize(scenario);
            if (TableSelector.SelectItem != null)
                dict.Add("Table", TableSelector.SelectItem.Name);

            return dict;
        }
        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu);
            TableSelector.SelectItem =
                dataManager.DataCollections.FirstOrDefault(d => d.Name == docu["Table"].ToString());
            TableSelector.InformPropertyChanged("");
        }
            
    }
}
