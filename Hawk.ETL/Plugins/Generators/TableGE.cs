using System;
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
    [XFrmWork("TableGE","TableGE_desc" )]
    public class TableGE : GeneratorBase
    {
        private readonly IDataManager dataManager;

        public TableGE()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["DataManager"] as IDataManager;
            TableSelector = new ExtendSelector<string>();
            TableSelector.GetItems = () => dataManager.DataCollections.Select(d=>d.Name).ToList();
            TableSelector.SelectChanged +=(s,e)=> this.InformPropertyChanged("TableSelector");
        }

     

        [Browsable(false)]
        public override string KeyConfig => TableSelector?.SelectItem; 
        [LocalizedDisplayName("table")]
        [LocalizedDescription("key_462")]
        [PropertyOrder(1)]
        public ExtendSelector<string> TableSelector { get; set; }

        //public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        //{
        //    var dict = base.DictSerialize(scenario);
        //    if(TableSelector.SelectItem!=null)
        //      dict.Set("Table", TableSelector.SelectItem);
             
        //    return dict;
        //}
        //public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        //{
        //    base.DictDeserialize(docu);
        //    TableSelector.SelectItem =
        //        dataManager.DataCollections.FirstOrDefault(d => d.Name == docu["Table"].ToString());
        //    TableSelector.InformPropertyChanged("");
        //}
        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            DataCollection table = this.Father.SysProcessManager.GetCollection(this.TableSelector.SelectItem);
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