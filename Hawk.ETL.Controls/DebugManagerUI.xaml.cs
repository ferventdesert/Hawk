using System.Windows.Controls;
using Hawk.Core.Utils.Plugins;
namespace Hawk.ETL.Controls

{ 


    /// <summary>
    /// DebugManagerUI.xaml 的交互逻辑
    /// </summary>
   [XFrmWork("DebugManager")]
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