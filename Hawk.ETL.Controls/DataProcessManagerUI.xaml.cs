using System.Windows.Controls;
using System.Windows.Input;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    /// <summary>
    /// DataProcessManagerUI.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("DataProcessManager")]
    public partial class DataProcessManagerUI : UserControl, ICustomView
    {
        #region Constants and Fields

        #endregion

        #region Constructors and Destructors

        public DataProcessManagerUI()
        {
            this.InitializeComponent();

        }


        #endregion

        #region Properties



        #endregion
 

        #region Methods



     

        #endregion

        public FrmState FrmState => FrmState.Large;
    }
}