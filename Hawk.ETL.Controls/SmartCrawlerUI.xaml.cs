using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    /// <summary>
    /// SmartCrawlerUI.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("网页采集器" )]
    public partial class SmartCrawlerUI : UserControl, ICustomView
    {
        public SmartCrawlerUI()
        {
            InitializeComponent();


        }


        public FrmState FrmState
        {
            get { return FrmState.Large; }
        }
    }
}
