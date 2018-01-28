using System.Windows.Controls;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    /// <summary>
    ///     SubTaskView.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("子任务面板")]
    public partial class SubTaskView : UserControl, ICustomView
    {
        public SubTaskView()
        {
            InitializeComponent();
        }

        public FrmState FrmState => FrmState.Middle;
    }
}