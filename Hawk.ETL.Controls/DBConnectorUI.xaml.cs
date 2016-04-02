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
            listBoxDataCollection.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var attr = this.listBoxDataCollection.SelectedItem;

                    if (attr == null)
                    {
                        return;
                    }

                    var data = new DataObject(typeof(IDictionarySerializable), attr);
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                }
            };


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