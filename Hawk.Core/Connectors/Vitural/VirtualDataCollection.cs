using System.Collections.Generic;
using System.ComponentModel;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors.Vitural
{
    public class VirtualDataCollection : DataCollection
    {
        protected VirtualizingCollection<IDictionarySerializable> VirtualData;


        public VirtualDataCollection()
        {
        }

        public VirtualDataCollection(IItemsProvider<IDictionarySerializable> data,  int pageTimeout = 30000)
            : base()
        {
         
            VirtualData = new VirtualizingCollection<IDictionarySerializable>(data,pageTimeout);
 
            data.AlreadyGetSize += (s, e) => OnPropertyChanged("Count");
        }

        public override string Source => VirtualData.ItemsProvider.Name;

        [DisplayName("虚拟化数据集")]
        public override bool IsVirtual => true;


        public IItemsProvider<IDictionarySerializable> ItemsProvider => VirtualData.ItemsProvider;

        [Browsable(false)]
        public override IList<IDictionarySerializable> ComputeData => VirtualData;
    }


}