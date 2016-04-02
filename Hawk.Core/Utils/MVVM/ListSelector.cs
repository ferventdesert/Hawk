using System.Windows;
using System.Windows.Controls;

namespace Hawk.Core.Utils.MVVM
{
    public class ListSelector
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
            DependencyProperty.RegisterAttached("Property", typeof(string), typeof(ListSelector), new FrameworkPropertyMetadata("",
                    new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (FrameworkElement)d;
                var prop = GetProperty(d);
            var select = WPFOperate.FindVisualParent<ListBoxItem>(button);
         var dataContext=   select.DataContext;
            select.MouseLeftButtonUp+=(s2,e2)=>
                {
                    var info =  dataContext.GetType().GetProperty(prop);
                    var va= (bool)info.GetValue(dataContext,new object[]{});
                    va = !va;
                    info.SetValue(dataContext,va,new object[]{});


                };
           
        }

    }
}
