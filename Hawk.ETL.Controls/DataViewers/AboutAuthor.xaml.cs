using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls.DataViewers
{
    /// <summary>
    /// AboutAuthor.xaml 的交互逻辑
    /// </summary>
     [XFrmWork("key_263")]
    public partial class AboutAuthor : UserControl, ICustomView
    {
        public AboutAuthor()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/ferventdesert/Hawk/wiki/8.%E5%85%B3%E4%BA%8E%E4%BD%9C%E8%80%85";
            System.Diagnostics.Process.Start(url);
        }

        public FrmState FrmState { get; }
    }
}
