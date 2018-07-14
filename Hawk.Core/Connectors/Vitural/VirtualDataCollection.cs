using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors.Vitural
{
    public class VirtualDataCollection : DataCollection
    {
        protected VirtualizingCollection<IFreeDocument> VirtualData;


        public VirtualDataCollection()
        {
        }

        public VirtualDataCollection(IItemsProvider<IFreeDocument> data,  int pageSize = 1000)
            : base()
        {
         
            VirtualData = new VirtualizingCollection<IFreeDocument>(data,pageSize);
 
            data.AlreadyGetSize += (s, e) => OnPropertyChanged("Count");
        }

        public override string Source => VirtualData.ItemsProvider.Name;

        [LocalizedDisplayName("虚拟化数据集")]
        public override bool IsVirtual => true;


        public IItemsProvider<IFreeDocument> ItemsProvider => VirtualData.ItemsProvider;

        [Browsable(false)]
        public override IList<IFreeDocument> ComputeData => VirtualData;
    }


}