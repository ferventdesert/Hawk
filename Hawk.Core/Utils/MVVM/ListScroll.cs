using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Hawk.Core.Utils.MVVM
{
    public class ListScroll
    {
        #region IsEnabled

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ListScroll),
                new FrameworkPropertyMetadata((bool)false,
                    new PropertyChangedCallback(OnIsEnabledChanged)));

        public static bool GetIsEnabled(ItemsControl d)
        {
            return (bool)d.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(ItemsControl d, bool value)
        {
            d.SetValue(IsEnabledProperty, value);
        }


 

        

        
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            bool oldIsEnabled = (bool)e.OldValue;
            bool newIsEnabled = (bool)d.GetValue(IsEnabledProperty);
          
            var itemsControl = d as ItemsControl;
            if (itemsControl == null)
                return;

            if (newIsEnabled)
            {
                itemsControl.Loaded += (ss, ee) =>
                {
                    ScrollViewer scrollviewer = WPFOperate.GetChild<ScrollViewer>(itemsControl);
                    if (scrollviewer != null)
                    {
                        ((ICollectionView)itemsControl.Items).CollectionChanged += (sss, eee) => scrollviewer.ScrollToEnd();
                    }
                };
            }
          
        }

        #endregion


    }

    public class ListScrollSelect
    {
        #region IsEnabled

       


        public static readonly DependencyProperty IsToSelectedEnabledProperty =
            DependencyProperty.RegisterAttached("IsToSelectedEnabled", typeof(bool), typeof(ListScrollSelect),
                new FrameworkPropertyMetadata((bool)false,
                    new PropertyChangedCallback(OnIsEnabledChanged)));

        public static bool GetIsToSelectedEnabled(ListView d)
        {
            return (bool)d.GetValue(IsToSelectedEnabledProperty);
        }

        public static void SetIsToSelectedEnabled(ListView d, bool value)
        {
            d.SetValue(IsToSelectedEnabledProperty, value);
        }

 



        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            bool oldIsEnabled = (bool)e.OldValue;
           
            bool newIsSelected = (bool)d.GetValue(IsToSelectedEnabledProperty);
            var listView = d as ListView;
            if (listView == null)
                return;

            
            if (newIsSelected)
            {

                if (listView != null)
                {
                    listView.SelectionChanged += (s, e2) =>
                    {
                        if (listView.Tag.ToString() == "True")
                            listView.ScrollIntoView(listView.SelectedItem);
                       
                    };
                }

            }
        }

        #endregion


    }


}