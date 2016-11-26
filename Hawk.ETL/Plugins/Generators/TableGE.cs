using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Generators
{
    [XFrmWork("从数据表生成","从数据管理中已有的数据表中生成" )]
    public class TableGE : GeneratorBase
    {
        private readonly IDataManager dataManager;

        public TableGE()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["数据管理"] as IDataManager;
            TableSelector = new ExtendSelector<DataCollection>();
            TableSelector.GetItems = () => dataManager.DataCollections.ToList();
        }

        [LocalizedDisplayName("数据表")]
        [LocalizedDescription("选择所要连接的数据表")]
        [PropertyOrder(1)]
        public ExtendSelector<DataCollection> TableSelector { get; set; }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize(scenario);
            if(TableSelector.SelectItem!=null)
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
        public override IEnumerable<FreeDocument> Generate(IFreeDocument document = null)
        {
            DataCollection table = TableSelector.SelectItem;
            if(table==null)
                yield break;
            var me = table.ComputeData;
            foreach (IDictionarySerializable  item in me)
            {
                yield return item.Clone() as FreeDocument;
            }
        }



    }
}