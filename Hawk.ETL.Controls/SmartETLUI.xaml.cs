using System.Globalization;
using Hawk.Core.Utils;
using System.Windows.Data;
using System.Windows.Media;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;


    public class GroupColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var group = value.ToString();
            switch (group)
            {
              
           
                case "Input":
                    return new SolidColorBrush(new Color() {R=0,G=255,B=0,A=40});
                case "Output":
                    return new SolidColorBrush(new Color() { R = 0, G = 0, B = 255, A = 40 });
                default:
                    return new SolidColorBrush(new Color() { R = 255, G = 255, B = 255, A = 40 });

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ETLSmartView.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("SmartETLTool")]
    public partial class ETLSmartView : UserControl, ICustomView, IRemoteInvoke
    {
        public ETLSmartView()
        {
            this.InitializeComponent();
            this.ToolList.MouseDoubleClick += ToolList_MouseDoubleClick;
            SetTemplate();
            ConfigFile.GetConfig().PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsDisplayDetail")
                {
                    SetTemplate();
                }
          
            };
        }

        void SetTemplate()
        {
            var isdetail = ConfigFile.GetConfig().Get<bool>("IsDisplayDetail");
            var dataTemplate =
                isdetail ? "DataTemplateList"
                : "DataTemplateIcon";
            var panelTempate =
                isdetail ? "ItemsPanelTemplate2" : "ItemsPanelTemplate1";
            ETLToolList.ItemTemplate = this.FindResource(dataTemplate) as DataTemplate;
            ETLToolList.ItemsPanel = this.FindResource(panelTempate) as ItemsPanelTemplate;


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
                MessageBox.Show(GlobalHelper.Get("key_779"));

            }
        }

        public FrmState FrmState => FrmState.Large;

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

        private void Rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount ==1)
            {
                XLogSys.Print.Warn(GlobalHelper.Get("key_780"));
            }
        }
    }
}
