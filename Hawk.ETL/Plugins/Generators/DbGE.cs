using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Connectors.Vitural;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using IDataBaseConnector = Hawk.Core.Connectors.IDataBaseConnector;
using TableInfo = Hawk.Core.Connectors.TableInfo;

namespace Hawk.ETL.Plugins.Generators
{
    [XFrmWork("从数据库生成","从数据库读取内容，需提前在数据视图中配置连接","database" )]
    public class DbGE : GeneratorBase
    {
        private IDataManager dataManager;
        protected IDataBaseConnector hubbleDotNet;

        public DbGE()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["数据管理"] as IDataManager;
 
         
            ConnectorSelector=new ExtendSelector<IDataBaseConnector>();
            ConnectorSelector.GetItems = () => dataManager.CurrentConnectors.ToList();
            TableNames=new ExtendSelector<string>();
            Mount = -1;
            ConnectorSelector.SelectChanged += (s, e) => TableNames.SetSource(ConnectorSelector.SelectItem.RefreshTableNames().Select(d=>d.Name));
            TableNames.SelectChanged += (s, e) => { this.InformPropertyChanged("TableNames"); };
        }
        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("1.连接器")]
        [LocalizedDescription("选择所要连接的数据库服务")]
        [PropertyOrder(1)]
        public ExtendSelector<IDataBaseConnector> ConnectorSelector { get; set; }


        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("2.操作表名")]
        [PropertyOrder(2)]
        public ExtendSelector<string> TableNames { get; set; }


        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("3.数量")]
        [PropertyOrder(3)]
        public int Mount { get; set; }

 
 


        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            var mount = 0;
            if (Mount < 0)
                mount = int.MaxValue;
            var table =  this.ConnectorSelector.SelectItem?.RefreshTableNames().FirstOrDefault(d=>d.Name== TableNames.SelectItem);
            if (table != null)
            {
                var con = new VirtualDataCollection(table.GetVirtualProvider<IFreeDocument>());
                foreach (var item in con.ComputeData.Take(mount))
                {
                    if(item!=null)
                    yield return item;
                }
            }

      
         

        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            FreeDocument dict = base.DictSerialize(scenario);
            if (ConnectorSelector.SelectItem != null)
            {
                dict.Add("Connector", ConnectorSelector.SelectItem.Name);
            }
            if (TableNames.SelectItem != null)
            {
                dict.Add("Table", TableNames.SelectItem);
            }
          
            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu);
            ConnectorSelector.SetSource(dataManager.CurrentConnectors);
            ConnectorSelector.SelectItem =
                dataManager.CurrentConnectors.FirstOrDefault(d => d.Name == docu["Connector"].ToString());

            TableNames.SelectItem =
                docu["Table"].ToString();
        }
    }
}

