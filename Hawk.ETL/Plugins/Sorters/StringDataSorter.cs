using System;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Sorters
{
   // [XFrmWork("字符串排序" )]
    public class StringDataSorter : ColumnDataSorterBase
    {
        public override int Compare(IFreeDocument a, IFreeDocument b)
        {
            object a1 = a[Column];
            object b1 = b[Column];
            if (a1 == null || b1 == null)
                return 0;
            return String.CompareOrdinal(a1.ToString(), b1.ToString());
        }
    }
}