using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls.Themes
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
                   dict.Source = new Uri("Hawk.ETL.Controls;component/Themes/Icons.xaml", UriKind.RelativeOrAbsolute);
               }
               catch (Exception ex)
               {

                XLogSys.Print.Warn(ex);
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



    public class TextToFlowDocumentConverterPlus : DependencyObject, IValueConverter
    {
        public Markdown.Xaml.Markdown Markdown
        {
            get { return (Markdown.Xaml.Markdown)GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Markdown.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register("Markdown", typeof(Markdown.Xaml.Markdown), typeof(Markdown.Xaml.TextToFlowDocumentConverter), new PropertyMetadata(null));

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var text = (string)value;
            try
            {
                if (ConfigFile.GetConfig().Get<bool>("IsDisplayDetail") == true)
                {
                    text = text.Split('\n')[0];
                }
                var engine = Markdown ?? mMarkdown.Value;

                text = string.Join("\n", text.Trim().Split('\n').Select(d => d.Trim()));
                return engine.Transform(text);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Lazy<Markdown.Xaml.Markdown> mMarkdown
            = new Lazy<Markdown.Xaml.Markdown>(() => new Markdown.Xaml.Markdown());
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
