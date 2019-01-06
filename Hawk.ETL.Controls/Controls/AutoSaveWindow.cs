using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls.Controls
{
    [XFrmWork("auto_save_tooltip")]
    public class AutoSaveWindow : PopupWindowBase,ICustomView
    {
        static AutoSaveWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoSaveWindow), new FrameworkPropertyMetadata(typeof(AutoSaveWindow)));

        }

        public AutoSaveWindow()
        {
            var item = Application.Current.Resources["AutoSaveWindow"];
            this.DataContext = this;
            this.Style = item as Style;
        }

        public FrmState FrmState { get; }
    }
}
