using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    public class OpacityConverter : IMultiValueConverter
    {
        //正向修改，整合颜色值
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return 1;
            if (!(values[0] is int))
            {
                return 1;
            }
            var index = (int)values[0];
            var start = (int)values[1];
            var end = (int)values[2];
            if (index >= start && index <= end)
                return 1;
            return 0.5;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class DebugRemindConverter : IMultiValueConverter
    {
        //正向修改，整合颜色值
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Hidden;
            int pos;
            int total;
            if(!int.TryParse((values[0].ToString()),out pos))
                return Visibility.Hidden;
            if (!int.TryParse((values[1].ToString()), out total))
                return Visibility.Hidden;
            return pos<total?Visibility.Visible:Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    ///     SubTaskView.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("key_417")]
    public partial class SubTaskView : UserControl, ICustomView
    {
        public SubTaskView()
        {
            InitializeComponent();
        }

        public FrmState FrmState => FrmState.Middle;
    }
}