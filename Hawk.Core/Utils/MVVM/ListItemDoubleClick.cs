using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hawk.Core.Utils.MVVM
{
    public class ListBindingDoubleClick
    {
        public static object GetProperty(DependencyObject obj)
        {
            return (object)obj.GetValue(PropertyProperty);
        }

        public static void SetProperty(DependencyObject obj, object value)
        {
            obj.SetValue(PropertyProperty, value);
        }

        public static readonly DependencyProperty PropertyProperty =
            DependencyProperty.RegisterAttached("Property", typeof(object), typeof(ListBindingDoubleClick), new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            var listBoxItem = (FrameworkElement)d;
            if (listBoxItem == null)
                return;
            var v = GetProperty(d);
            var str = (ICommand)v;
           
            //TODO: never invoke the event
            listBoxItem.MouseDown += (s2, e2) =>
            {
                if (e2.ClickCount != 2) return;
                if (str.CanExecute(listBoxItem.DataContext))
                    str.Execute(listBoxItem.DataContext);
            };




        }
    }

    public  class ListItemDoubleClick
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
            DependencyProperty.RegisterAttached("Property", typeof(string), typeof(ListItemDoubleClick), new FrameworkPropertyMetadata("",
                    new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            var listBoxItem =  (FrameworkElement)d;
            if(listBoxItem==null)
                return;
            var str = GetProperty(d);
            var listbox = WPFOperate.FindVisualParent<ListBox>(listBoxItem);
            if (listbox == null)
                return;
            var dataContext = listbox.DataContext;
            listBoxItem.MouseDown += (s2, e2) =>
                {
                    if (e2.ClickCount != 2) return;
                    var command = (ICommand)BindingEvaluator.GetValue(dataContext, str);
                    if (command.CanExecute(listBoxItem.DataContext))
                        command.Execute(listBoxItem.DataContext);
                };
          
         
           

        }
    }


    public class ListViewItemDoubleClick
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
            DependencyProperty.RegisterAttached("Property", typeof(string), typeof(ListViewItemDoubleClick), new FrameworkPropertyMetadata("",
                    new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBoxItem = (FrameworkElement)d;
            if (listBoxItem == null)
                return;
            var str = GetProperty(d);
            var listbox = WPFOperate.FindVisualParent<ListView>(listBoxItem);
            if (listbox == null)
                return;
            var dataContext = listbox.DataContext;
            listBoxItem.MouseLeftButtonDown += (s2, e2) =>
            {
                if (e2.ClickCount != 2) return;
                var command = (ICommand)BindingEvaluator.GetValue(dataContext, str);
                if (command.CanExecute(listBoxItem.DataContext))
                    command.Execute(listBoxItem.DataContext);
            };




        }
    }
    public class TreeViewItemDoubleClick
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
            DependencyProperty.RegisterAttached("Property", typeof(string), typeof(TreeViewItemDoubleClick), new FrameworkPropertyMetadata("",
                    new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBoxItem = (FrameworkElement)d;
            if (listBoxItem == null)
                return;
            var str = GetProperty(d);
            var listbox = WPFOperate.FindVisualParent<TreeView>(listBoxItem);
           
            if (listbox == null)
                return;
            var dataContext = listbox.DataContext;
         
            listBoxItem.MouseLeftButtonDown += (s2, e2) =>
            {
                if (e2.ClickCount != 2) return;
                var command = (ICommand)BindingEvaluator.GetValue(dataContext, str);
                if (command.CanExecute(listBoxItem.DataContext))

                {
                      
                    command.Execute( listBoxItem.DataContext );
                       
                }
            };

                                      



        }
    }
}
