using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    /// <summary>
    /// DBConnectorUI.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("key_223")]
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


        private void ListBox_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var attr = this.dataListBox.SelectedItem;

                if (attr == null)
                {
                    return;
                }

                var data = new DataObject(typeof(IDictionarySerializable), attr);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            }
        }
        public FrmState FrmState
        {
            get
            {
                return FrmState.Large;
            }
        }
    }
}