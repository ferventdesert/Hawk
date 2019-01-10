using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
           if (Application.Current == null)
               return name;
            str = Application.Current.TryFindResource(name);
           if (str == null)
#if DEBUG
               // throw new  Exception(name+"not found");
               return name;
#else
                return name;
#endif
            return str.ToString();
        }

        public static string GetEnum(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description))) ? Get(attributes[0].Description) : Get(value.ToString());
        }

        private static  IEnumerable<string> GetResouceKeys(ResourceDictionary dict)
        {
            if (dict == null)
                yield break;
            foreach (var resource in dict.Keys)
            {
                var value = dict[resource];
                if (value is ResourceDictionary)
                {
                    var dict2 = value as ResourceDictionary;
                    foreach (var item in GetResouceKeys(dict2))
                    {
                        yield return item;
                    }
                }
                else if(value is string)
                {
                    yield return resource.ToString();
                }
            }
            foreach (var resourceDictionary in dict.MergedDictionaries)
            {
                foreach (var item in GetResouceKeys(resourceDictionary))
                {
                    yield return item;
                }
            }

        } 
        public static List<string> GetWithStart(string startswith)
        {
            List<string> result = null;
            if (!startsBuff.TryGetValue(startswith, out result))
            {
                result=new List<string>();
                result.AddRange(from object item in GetResouceKeys(Application.Current.Resources) where item.ToString().StartsWith(startswith) select item.ToString());
                startsBuff[startswith] = result;

            }
            return result;
         
        }

        private static Dictionary<string, List<string>> startsBuff = new Dictionary<string, List<string>>(); 

        private static  Random random = new Random();
        public static string GetWithRandom(string startswith)
        {

            var items = GetWithStart(startswith);
            if (items.Count == 0)
                return null;
            return items[random.Next(0, items.Count)];
        }
    
        public static string FormatArgs(params object[] values)
        {
            var format = values[0];
            // Get localized version of the default language string:
            var localFormat = Get(format.ToString());
            // Feed the resulting format string into String.Format:
            return string.Format(localFormat, values.Skip(1).ToArray());
        }
        public static string RandomFormatArgs(params object[] values)
        {
            var format = values[0].ToString();
            var item = GetWithRandom(format);
            if (item == null)
                return format;

            // Get localized version of the default language string:
            var localFormat = Get(item);
            // Feed the resulting format string into String.Format:
            return string.Format(localFormat, values.Skip(1).ToArray());
        }
    }
}
