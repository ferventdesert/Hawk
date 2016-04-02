using System.Windows;
using System.Windows.Input;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Utils.MVVM
{
    public static class DropHelper
    {
        #region Fields

        public static readonly DependencyProperty DropSupportProperty = 
            DependencyProperty.RegisterAttached("DropSupport", typeof(DropSupportData), typeof(DropHelper),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnDropSupportChanged)));

        #endregion Fields

        #region Methods

        public static DropSupportData GetDropSupport(DependencyObject d)
        {
            return (DropSupportData)d.GetValue(DropSupportProperty);
        }

        public static void SetDropSupport(DependencyObject d, DropSupportData value)
        {
            d.SetValue(DropSupportProperty, value);
        }

        static void ff_Drop(object sender, DragEventArgs e)
        {
            var dropdata = GetDropSupport((DependencyObject)sender);
            if (dropdata != null)
            {
                var ff = (sender) as FrameworkElement;
                if (ff == null)
                    return;

                var cmd = BindingEvaluator.GetValue(ff.DataContext, dropdata.BindingPath) as ICommand;
                if (cmd != null)
                {
                    if (!string.IsNullOrEmpty(dropdata.DataFormat))
                    {
                        cmd.Execute(e.Data.GetData(dropdata.DataFormat));
                    }
                    else
                    {
                        var data = e.Data.GetData(typeof(IDictionarySerializable));
                        cmd.Execute(data);
                    }

                }
            }
        }

        static void ff_PreviewDragOver(object sender, DragEventArgs e)
        {
            var dropdata = GetDropSupport((DependencyObject)sender);
            if (dropdata != null)
            {
                if (!string.IsNullOrEmpty(dropdata.DataFormat))
                {
                    if (e.Data.GetDataPresent(dropdata.DataFormat))
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                    e.Handled = true;
                }
              
            }
        }

        private static void OnDropSupportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropSupportData oldDropSupport = (DropSupportData)e.OldValue;
            DropSupportData newDropSupport = (DropSupportData)d.GetValue(DropSupportProperty);

            if (d is FrameworkElement == false)
                return;
            var ff = (FrameworkElement)d;

            if (newDropSupport != null && newDropSupport.BindingPath != null)
            {
                ff.AllowDrop = true;

                ff.Drop += ff_Drop;
                ff.PreviewDragOver += ff_PreviewDragOver;
            }
            else
            {
                ff.AllowDrop = false;

                ff.Drop -= ff_Drop;
                ff.PreviewDragOver -= ff_PreviewDragOver;
            }
        }

        #endregion Methods
    }

    public class DropSupportData
    {
        #region Properties

        public string BindingPath
        {
            get;
            set;
        }

        public string DataFormat
        {
            get;
            set;
        }

        #endregion Properties
    }
}