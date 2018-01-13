using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Utils
{
    public class GroupConverter : IValueConverter
    {
        public static Dictionary<string, string> map = new Dictionary<string, string>
        {
            {"IColumnDataSorter", "排序"},
            {"IColumnAdviser", "顾问"},
            {"IColumnGenerator", "生成"},
            {"IColumnDataFilter", "过滤"},
            {"IColumnDataTransformer", "转换"},
            {"IDataExecutor", "执行"}
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as XFrmWorkAttribute;
            var unknown = "未知";
            if (s == null)
                return unknown;
            var p = s.MyType;
            if(p==null)
                return unknown;
            foreach (var item in map)
            {
                if (p.GetInterface(item.Key) != null)
                    return item.Value;
            }
            return unknown;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
