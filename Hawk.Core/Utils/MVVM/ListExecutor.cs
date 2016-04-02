using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Hawk.Core.Utils.MVVM
{
    public class ListExecutor
    {

        public static string GetProperty(DependencyObject obj)
        {
            return (string)obj.GetValue(PropertyProperty);
        }

        public static void SetProperty(DependencyObject obj, string value)
        {
            obj.SetValue(PropertyProperty, value);
        }

        public static readonly DependencyProperty PropertyProperty =
            DependencyProperty.RegisterAttached("Property", typeof(string), typeof(ListExecutor), new FrameworkPropertyMetadata("",
                    new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (ButtonBase)d;
            if (button == null)
                return;

            var str = GetProperty(d);
            if (str == null)
                return;
            var select = WPFOperate.FindVisualParent<ListBoxItem>(button);

            var listbox = WPFOperate.FindVisualParent<ListBox>(select);
            if (listbox == null)
                return;
            button.DataContext = BindingEvaluator.GetValue(listbox.DataContext, str);
            button.Click += (s2, e2) =>
            {
                var command = (ICommand)BindingEvaluator.GetValue(listbox.DataContext, str);
                if (command.CanExecute(@select.DataContext))
                    command.Execute(@select.DataContext);
            };
        }
    }

   
    public class ListViewExecutor
    {

        public static string GetProperty(DependencyObject obj)
        {
            return (string)obj.GetValue(PropertyProperty);
        }

        public static void SetProperty(DependencyObject obj, string value)
        {
            obj.SetValue(PropertyProperty, value);
        }

        public static readonly DependencyProperty PropertyProperty =
            DependencyProperty.RegisterAttached("Property", typeof(string), typeof(ListViewExecutor), new FrameworkPropertyMetadata("",
                    new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (ButtonBase)d;
            var str = GetProperty(d);
            var select = WPFOperate.FindVisualParent<ListViewItem>(button);
            var listbox = WPFOperate.FindVisualParent<ListView>(select);
            button.DataContext = BindingEvaluator.GetValue(listbox.DataContext, str);
                if (button != null)
                {
                    button.Click += (s2, e2) =>
                    {


                        var command = (ICommand)BindingEvaluator.GetValue(listbox.DataContext, str);
                        if (command.CanExecute(select.DataContext))
                            command.Execute(select.DataContext);
                    };
                }
           

        }


    }
}
