using System.Windows.Controls;
using Hawk.Core.Utils.Plugins;
namespace Hawk.ETL.Controls

{ 


    /// <summary>
    /// DebugManagerUI.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("调试信息窗口",  "输出调试信息", "")]
    public partial class DebugManagerUI : UserControl, ICustomView
    {
        #region Constructors and Destructors

        public DebugManagerUI()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Properties

        public FrmState FrmState => FrmState.Buttom;

        #endregion
    }
}