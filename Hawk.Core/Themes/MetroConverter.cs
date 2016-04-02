using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Hawk.Core.Themes
{
    public class MetroConverter:IValueConverter
    {
        private static ResourceDictionary dict;

        private  static  Dictionary<object, object> foundIcons; 
           static  MetroConverter()
        {
            dict = new ResourceDictionary {};
               try


               {
                   dict.Source = new Uri("Hawk.Core;component/Themes/Icons.xaml", UriKind.RelativeOrAbsolute);
               }
               catch (Exception ex)
               {
                   
                    
               }
              foundIcons = dict
                .OfType<DictionaryEntry>()
                .Where(de => de.Value is Canvas).ToDictionary(d=> d.Key,d=>d.Value);
                
               
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (foundIcons == null) return null;
            if (value == null) return foundIcons.Values.FirstOrDefault();
            if (foundIcons.ContainsKey("appbar_" + value))
                return foundIcons["appbar_" + value];
            return foundIcons.Values.FirstOrDefault();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
