using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("写入数据表", "将数据保存至软件的数据管理器中，之后可方便进行其他处理，拖入到任意一列皆可")]
    public class TableEX : DataExecutorBase
    {
        private readonly IDataManager dataManager;
        private DataCollection collection;

        public TableEX()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["数据管理"] as IDataManager;
        }

        [LocalizedDisplayName("克隆副本")]
        [LocalizedDescription("将当前数据复制后保存到数据表中，可防止后续工作模块对数据的修改")]
        public bool IsClone { get; set; }

        [LocalizedDisplayName("新建表名")]
        [LocalizedDescription("如果要新建表，则填写此项，否则留空")]
        public string Table { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            collection = dataManager.DataCollections.FirstOrDefault(d => d.Name == Table);
            if (collection == null && string.IsNullOrEmpty(Table) == false)

            {
                collection = new DataCollection(new List<IFreeDocument>()) {Name = Table};
                dataManager.AddDataCollection(collection);
            }

            return base.Init(datas);
        }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            if (collection == null)
                yield break;
            foreach (var computeable in documents)
            {
                ControlExtended.UIInvoke(() =>
                {
                    var data = IsClone ? computeable.Clone() : computeable;
                    collection.ComputeData.Add(data);
                    collection.OnPropertyChanged("Count");
                });

                yield return computeable;
            }
        }
    }
}