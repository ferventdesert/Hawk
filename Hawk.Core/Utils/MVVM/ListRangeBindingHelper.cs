using System.Collections;
using System.Windows;

namespace Hawk.Core.Utils.MVVM
{
    public class ListRangeBindingHelper
    {
        #region Constants and Fields

        public static readonly DependencyProperty RangeProperty = DependencyProperty.RegisterAttached(
            "Range", typeof(string), typeof(ListRangeBindingHelper), new PropertyMetadata("", OnPropertyChanged));

        #endregion

        #region Public Methods

        public static string GetRange(DependencyObject obj)
        {
            return (string)obj.GetValue(RangeProperty);
        }

        public static void SetRange(DependencyObject obj, string value)
        {
            obj.SetValue(RangeProperty, value);
        }

        #endregion

        // Using a DependencyProperty as the backing store for Range.  This enables animation, styling, binding, etc...

        #region Methods

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var b = (FrameworkElement)d;
            b.DataContextChanged += b_DataContextChanged;
        }

        private static void b_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var item = sender as FrameworkElement;
            string range = GetRange(item);
            string[] ranges = range.Split('-');
            if (range.Length != 2)
            {
                return;
            }
            var enumerable = item.DataContext as IEnumerable;
            item.DataContextChanged -= b_DataContextChanged;
            int start = int.Parse(ranges[0]);
            int end= int.Parse(ranges[1]);

            item.DataContext = enumerable.GetRange(start,end);
            item.DataContextChanged += b_DataContextChanged;
        }

        #endregion
    }
}