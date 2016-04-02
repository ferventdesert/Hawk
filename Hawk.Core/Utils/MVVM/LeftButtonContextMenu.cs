using System.Windows;
using System.Windows.Controls.Primitives;

namespace Hawk.Core.Utils.MVVM
{
    public class LeftButtonContextMenu
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
            DependencyProperty.RegisterAttached("Property", typeof(string), typeof(LeftButtonContextMenu), new FrameworkPropertyMetadata(null,
                    OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var b = (FrameworkElement)d;
          
                
                b.MouseLeftButtonDown += (s, e2) =>
                    {
                          if (b.ContextMenu != null)
                          {
                              var menu = b.ContextMenu;
                              //目标
                              menu.PlacementTarget = b;
                              //位置
                              menu.Placement = PlacementMode.Top;
                              //显示菜单
                            
                              menu.IsOpen = true;
                          }
                    };
      
            
        }
    }
}
