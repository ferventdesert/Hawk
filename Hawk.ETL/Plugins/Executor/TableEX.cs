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
    [XFrmWork("TableEX", "TableEX_desc", "column_three")]
    public class TableEX : DataExecutorBase
    {
        private readonly IDataManager dataManager;
        private DataCollection collection;

        public TableEX()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["DataManager"] as IDataManager;
        }
        [Browsable(false)]
        public override string KeyConfig => Table;

        [LocalizedDisplayName("key_22")]
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
            foreach (var computeable in documents)
            {
                if (collection != null)
                {
                    ControlExtended.UIInvoke(() =>
                    {

                        var data = computeable.Clone();
                        collection.ComputeData.Add(data);
                        collection.OnPropertyChanged("Count");
                    });
                }
               

                yield return computeable;
            }
        }
    }
}