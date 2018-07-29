using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls
{
    /// <summary>
    /// SystemStateViewer.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("key_794")]
    public partial class SystemStateViewer : UserControl, ICustomView
    {
        #region Constructors and Destructors

        public SystemStateViewer()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Properties

        public FrmState FrmState => FrmState.Middle;

        #endregion

     

        private void processListBox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var attr = this.processListBox.SelectedItem as IProcess;
                if (attr == null)
                {
                    return;
                }

                var data = new DataObject(typeof(IDictionarySerializable), attr);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            }
        }
    }
}