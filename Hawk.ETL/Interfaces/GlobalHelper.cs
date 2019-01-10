using System;
using System.Linq;
using System.Windows;

namespace Hawk.ETL.Interfaces
{
    /// <summary>
    /// 多国语言处理
    /// </summary>
   public class GlobalHelper
    {
       public static string Get(string name)
       {
            object str = null;
            str = Application.Current.TryFindResource(name);
            if (str == null)
                return name;
            return str.ToString();
        }
        public static string Format( FormattableString fs)
        {
            // Get localized version of the default language string:
            var localFormat = Get(fs.Format);
            // Feed the resulting format string into String.Format:
            return string.Format(localFormat, fs.GetArguments());
        }
        public static string FormatArgs(params object[] values)
        {
            var format = values[0];
            // Get localized version of the default language string:
            var localFormat = Get(format.ToString());
            // Feed the resulting format string into String.Format:
            return string.Format(localFormat, values.Skip(1).ToArray());
        }
    }
}
