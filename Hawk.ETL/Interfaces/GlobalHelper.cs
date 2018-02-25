using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Hawk.Core.Utils
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
    }
}
