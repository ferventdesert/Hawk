using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Sorters
{
    //[XFrmWork("基本数字排序" )]
    public class NumberColumnDataSorter : ColumnDataSorterBase
    {
        public override int Compare(IFreeDocument a, IFreeDocument b)
        {
            var a1 = a[Column];
            var b1 = b[Column];
            var res1 = false;
           var n1=      (double)AttributeHelper.ConvertTo(a1, SimpleDataType.DOUBLE, ref res1);
            if (res1 == false)
                return 0;
            var n2 = (double) AttributeHelper.ConvertTo(b1, SimpleDataType.DOUBLE, ref res1);
            if (res1 == false)
                return 0;
            return (int)(n1 - n2);
        }
    }
}