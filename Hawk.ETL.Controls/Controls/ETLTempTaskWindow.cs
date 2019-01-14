using System;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Controls.Controls
{
    [XFrmWork("etl_temp_window")]
    public class ETLTempTaskWindow : PopupWindowBase, ICustomView
    {
        static ETLTempTaskWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ETLTempTaskWindow), new FrameworkPropertyMetadata(typeof(ETLTempTaskWindow)));

        }   

        public ETLTempTaskWindow()
        {
            var item = Application.Current.Resources["ETLTempTaskWindow"];
            this.DataContext = this;
            this.Style = item as Style;
            Refresh = false;
        }

        public bool Refresh { get; set; }
        public FrmState FrmState { get; }

        public override ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                        {

                             new Command(GlobalHelper.Get( "key_172"),  obj=>ButtonClick1(),obj=>AllowOK) { Icon = "check" },
                              new Command(GlobalHelper.Get("key_142"),obj=>ButtonClick2(),obj=>AllowCancel) { Icon = "redo" },
                            new Command(GlobalHelper.Get("key_293"),obj=>ButtonClick3(),obj=>true) { Icon = "cancel" },
                        });
            }
        }
        protected override void ButtonClick3()
        {
            this.DialogResult = false;
            this.Close();

        }
        protected override  void ButtonClick2()
        {
            this.token.Cancel();
            Refresh = true;
            this.Close();

        }


    }
}
