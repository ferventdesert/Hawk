using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;

namespace Hawk.Core.Utils.Plugins
{
    [XFrmWork("属性配置器", "属性配置选项", "")]
    public class XFrmWorkPropertyGrid : AbstractPlugIn 
    {
         WPFPropertyGrid propertyGrid;

        private UserControl control;
        public XFrmWorkPropertyGrid()
        {
            if (MainDescription.IsUIForm)
                control = new UserControl();



        }


        public void SetObjectView(object obj)
        {
            if (!MainDescription.IsUIForm)
            {

                return;
            }
            if (propertyGrid == null)
            {
                this.propertyGrid = PropertyGridFactory.GetInstance(obj);
                propertyGrid.ShowReadOnlyProperties = true;
                control.Content = propertyGrid;
            }
            else
            {
                propertyGrid.SetObjectView(obj);
            }
            var dockableManager = this.MainFrmUI as IDockableManager;
            dockableManager?.ActiveThisContent(this.control);
        }

        public object UserControl => control;


        public bool ShowReadOnlyProperties
        {
            get
            {
                return propertyGrid.ShowReadOnlyProperties;
            }
            set
            {
                propertyGrid.ShowReadOnlyProperties = value;
            }
        }



        public FrmState FrmState => FrmState.Middle;
    }
}
