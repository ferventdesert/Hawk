using System.Collections.Generic;
using Hawk.Standard.Interfaces;
using Hawk.Standard.Plugins.Transformers;
using Hawk.Standard.Utils.Plugins;

namespace Hawk.Standard.Plugins.Sorters
{
    public class ColumnDataSorterBase :ToolBase, IColumnDataSorter
    {
        public ColumnDataSorterBase()
        {
            Column = "";
            Enabled = true;
           
        }

        

        public override  FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        { 
            var dict = base.DictSerialize();
            dict.Add("Group", GlobalHelper.Get("key_104"));
            return dict;
        }

   
     

        [LocalizedCategory("key_211")]
        [LocalizedDisplayName("key_466")]
        public SortType SortType { get; set; }

     




        public virtual int Compare(IFreeDocument a, IFreeDocument b)
        {
            return 0;
        }

        public virtual bool Init(IList<IFreeDocument> datas)
        {
            return false;
        }

   


        public int Compare(object x, object y)
        {
            var a = x as IFreeDocument;
            if (a == null) return 0;
            var b = y as IFreeDocument;
            if (b == null) return 0;
            return Compare(a, b);
        }
    }
}