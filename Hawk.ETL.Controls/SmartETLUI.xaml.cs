using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

 

    /// <summary>
    /// ETLSmartView.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("数据清洗ETL" )]
    public partial class ETLSmartView : UserControl, ICustomView, IRemoteInvoke
    {
        public ETLSmartView()
        {
            this.InitializeComponent();
            this.ToolList.MouseDoubleClick += ToolList_MouseDoubleClick;
        }

        void ToolList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var attr = this.ToolList.SelectedItem as XFrmWorkAttribute;
                if (attr == null)
                {
                    return;
                }

                MessageBox.Show("可将图标拖入右侧数据列的上方空白列表处，为该列添加ETL模块");

            }
        }

        public FrmState FrmState
        {
            get { return FrmState.Large; }
        }

        private void ListBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var dataObject = e.Data as DataObject;
            if (dataObject == null)
                return;
            if (dataObject.GetDataPresent(typeof(XFrmWorkAttribute)))
            {
                var rc = (XFrmWorkAttribute)dataObject.GetData(typeof(XFrmWorkAttribute));

                if (RemoteFunc != null)
                {
                    var frameworkElement = sender as FrameworkElement;
                    if (frameworkElement != null)
                    {
                        var da = frameworkElement.DataContext;
                        RemoteFunc("Drop",new[]{da,rc});
                    }
                }
            }
        }

        public Func<string, object, bool> RemoteFunc { get; set; }

     
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (RemoteFunc != null)
            {
                var frameworkElement = sender as FrameworkElement;
                if (frameworkElement != null)
                {
                    this.RemoteFunc("Click", frameworkElement.DataContext);
                }
            }
        }

        private void ButtonDelete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (RemoteFunc != null)
            {
                var frameworkElement = sender as FrameworkElement;
                if (frameworkElement != null)
                {
                    this.RemoteFunc("Delete", frameworkElement.DataContext);
                }
            }
        }
    }
}
