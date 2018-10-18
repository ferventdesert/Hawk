using System;
using System.Collections.Generic;
using Hawk.Standard.Interfaces;
using Hawk.Standard.Utils;
using Hawk.Standard.Utils.Plugins;

namespace Hawk.Standard.Plugins.Executor
{
    [XFrmWork("TableEX", "TableEX_desc", "column_three")]
    public class TableEX : DataExecutorBase
    {
        private readonly IDataManager dataManager;
        private DataCollection collection;

        public TableEX()
        {
        }
        [Browsable(false)]
        public override string KeyConfig => Table;

        [LocalizedDisplayName("key_22")]
        public string Table { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            collection = dataManager.DataCollections.FirstOrDefault(d => d.Name == Table);
            if (string.IsNullOrEmpty(Table))
                throw new ArgumentNullException("Table is empty");
            return base.Init(datas);
        }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            foreach (var computeable in documents)
            {
                if (collection == null)
                {

                    if (string.IsNullOrEmpty(Table) == false)

                    {
                        collection = new DataCollection(new List<IFreeDocument>()) { Name = Table };
                        dataManager.AddDataCollection(collection);
                    }

                }
                else
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