using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Hawk.Core.Utils.MVVM;
using log4net.Repository.Hierarchy;
using log4net;
using log4net.Core;

namespace Hawk.Core.Utils.Plugins
{
    [XFrmWork("key_4",  "DebugManager_desc", "")]
    public class DebugManager : AbstractPlugIn, IView, IMainFrmMenu
    {
        #region Properties

        public IAction BindingCommands => null;

        public FrmState FrmState => FrmState.Buttom;

        public object UserControl => null;

        #endregion
    }
}