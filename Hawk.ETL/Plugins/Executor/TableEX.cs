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
        }

       


        [DisplayName("新建表名")]
        [Description("如果要新建表，则填写此项，否则留空")]
        public string Table { get; set; }

        private DataCollection collection;

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
             collection = dataManager.DataCollections.FirstOrDefault(d => d.Name == Table);
            if (collection == null&&string.IsNullOrEmpty(Table)==false)

            {
                collection = new DataCollection(new List<IFreeDocument>()) { Name = Table };
                dataManager.AddDataCollection(collection);
            }

            return base.Init(datas);
        }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
          
            if(collection==null)
                yield break;
            foreach (IFreeDocument computeable in documents)
            {
                ControlExtended.UIInvoke(() => {
                    collection.ComputeData.Add(computeable);
                    collection.OnPropertyChanged("Count");
                });
             
                yield return computeable;
            }
        
              
           
          
        }

   
            
    }
}
