using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    /// <summary>
    /// DBConnectorUI.xaml 的交互逻辑
    /// </summary>
  //  [XFrmWork("数据管理")]
    public partial class DBConnectorUI : UserControl, ICustomView
    {
        #region Constants and Fields


        #endregion

        #region Constructors and Destructors

        public DBConnectorUI()
        {
            this.InitializeComponent();
         


        }

        #endregion

        #region Methods


        #endregion

        
       
        public FrmState FrmState
        {
            get
            {
                return FrmState.Large;
            }
        }
    }
}