using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Hawk.Core.Utils.Plugins;

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
            if (!(value is string))
                value = parameter;
            if (value == null)
                return foundIcons["appbar_base"];
            if (foundIcons.ContainsKey("appbar_" + value))
                return foundIcons["appbar_" + value];
            return foundIcons["appbar_base"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ModuleMetroConverter : IValueConverter
    {
        MetroConverter metro =new MetroConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return metro.Convert(Convert(value),null,null,null);

        }

        private string Convert(object value )
        {

            var item = (XFrmWorkAttribute)value;
            if (item == null)
                return "shuffle";
            var logo = item.LogoURL;
            var type = item.MyType.Name;
            if (string.IsNullOrEmpty(logo))
            {
                if (type.EndsWith("FT"))
                    return "filter";
                if (type.EndsWith("TF"))
                    return "deeplink";
                if (type.EndsWith("GE"))
                    return "diagram";
                if (type.EndsWith("EX"))
                    return "database";
            }
            else
            {
                return logo;
            }
            return "shuffle";

        }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class TaskMetroConverter : IValueConverter
    {
        MetroConverter metro = new MetroConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return metro.Convert(Convert(value), null, null, null);

        }

        private string Convert(object value)
        {

            var item = value.ToString();
            if(item== "SmartETLTool")


                return "diagram";
            else
            {
                return "camera";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
