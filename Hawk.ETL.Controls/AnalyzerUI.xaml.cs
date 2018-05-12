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

namespace Hawk.ETL.Controls
{
    /// <summary>
    /// AnalyzerUI.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("调试分析面板")]
    public partial class AnalyzerUI : UserControl,ICustomView
    {
        public AnalyzerUI()
        {
            InitializeComponent();
        }

        public FrmState FrmState {
            get { return FrmState.Large; }
        }
    }
}
