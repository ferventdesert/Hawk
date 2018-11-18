using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Utils
{

    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            ListViewItem item = (ListViewItem)value;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item) + 1;
            return index.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class AnalyzerEnterConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Brushes.Transparent;
            int count = (int)value;
            var brush = new SolidColorBrush(new Color() {B=190,A=100});
            return count > 0? brush : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class GroupConverter : IValueConverter
    {
        public static Dictionary<string, string> map = new Dictionary<string, string>
        {
            {"IColumnDataSorter", "key_104"},
            {"IColumnAdviser", "顾问"},
            {"IColumnGenerator", "key_106"},
            {"IColumnDataFilter", "key_107"},
            {"IColumnDataTransformer", "key_108"},
            {"IDataExecutor", "key_34"}
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as XFrmWorkAttribute;
            var unknown = "未知";
                if (s == null)
                return unknown;
            var p = s.MyType;
            if (s.Description.Contains(s.Name))
                return "常用";
            if (p == null)
                return unknown;
            foreach (var item in map)
            {
                if (p.GetInterface(item.Key) != null)
                    return GlobalHelper.Get( item.Value);
            }
            return unknown;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GeneratorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GenerateMode mode;

            if (Enum.TryParse(value?.ToString(), out mode))
            {
                var param = parameter?.ToString() == "True";
                if ((mode == GenerateMode.ParallelMode&&param)|| (mode == GenerateMode.SerialMode && param==false))
                   return Visibility.Visible;
            }
            return Visibility.Collapsed;


        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OnelineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var str = value.ToString();

            return str.Split('\n').Select(d=>d.Trim()).FirstOrDefault(d => d!="");



        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FlowDocumentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var str = value.ToString();
            var flowdoc = new FlowDocument();
            foreach (var s in str.Split('\n'))
            {

                Paragraph p = new Paragraph(new Run(s));
                flowdoc.Blocks.Add(p);
            }
            return flowdoc;



        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
