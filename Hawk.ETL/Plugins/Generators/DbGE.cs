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
    [XFrmWork("从连接器生成","从数据管理的连接器中生成序列" )]
    public class DbGE : GeneratorBase
    {
        private IDataManager dataManager;
        protected IDataBaseConnector hubbleDotNet;

        public DbGE()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["数据管理"] as IDataManager;
 
         
            ConnectorSelector=new ExtendSelector<IDataBaseConnector>();
            ConnectorSelector.GetItems = () => dataManager.CurrentConnectors.ToList();
            TableNames=new ExtendSelector<TableInfo>();
            Mount = -1;
            ConnectorSelector.SelectChanged += (s, e) => TableNames.SetSource(ConnectorSelector.SelectItem.RefreshTableNames());
        }

        [LocalizedDisplayName("连接器")]
        [LocalizedDescription("选择所要连接的数据库服务")]
        [PropertyOrder(1)]
        public ExtendSelector<IDataBaseConnector> ConnectorSelector { get; set; }


        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("操作表名")]
        public ExtendSelector<TableInfo> TableNames { get; set; }


        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("数量")]
        public int Mount { get; set; }

 
 


        public override IEnumerable<FreeDocument> Generate(IFreeDocument document = null)
        {
            var mount = 0;
            if (Mount < 0)
                mount = int.MaxValue;
            TableInfo table = TableNames.SelectItem;
            if (table != null)
            {
                var con = new VirtualDataCollection(table.GetVirtualProvider<IFreeDocument>());
                foreach (var item in con.ComputeData.Take(mount).Select(d => d.DictSerialize()))
                {
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
                dict.Add("Table", TableNames.SelectItem.Name);
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
                ConnectorSelector.SelectItem.RefreshTableNames().FirstOrDefault(d => d.Name == docu["Table"].ToString());
        }
    }
}

