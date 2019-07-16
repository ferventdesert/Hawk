using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using System.Diagnostics;
namespace Hawk.ETL.Managements
{
    public class CommandLineContainer : IMainFrm, IDockableManager
    {
        public CommandLineContainer()
        {
            PluginManager = new PluginManager();
            MainDescription.IsUIForm = false;
            MainDescription.MainFrm = this;
            PluginManager.MainFrmUI = this;
            var MainStartUpLocation =Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //var MainStartUpLocation = @"D:\TopCoder\Hawk\Bin";
            MainPluginLocation = MainStartUpLocation;
            PluginManager.Init(new[] { MainStartUpLocation });
            PluginManager.LoadPlugins();
        }

        public PluginManager PluginManager { get; set; }

        public void AddDockAbleContent(FrmState thisState, object thisControl, params string[] objects)
        {
        }

        public void RemoveDockableContent(object model)
        {
        }

        public void ActiveThisContent(object view)
        {
        }

        public void ActiveThisContent(string name)
        {
        }

        public event EventHandler<DockChangedEventArgs> DockManagerUserChanged;
        public List<ViewItem> ViewDictionary { get; }

        public void SetBusy(ProgressBarState state = ProgressBarState.Normal, string title = "系统正忙", string message = "正在处理长时间操作",
            int percent = 0)
        {
            throw new NotImplementedException();
        }

     


        public ObservableCollection<IAction> CommandCollection { get; set; }
        public string MainPluginLocation { get; }
        public Dictionary<string, IXPlugin> PluginDictionary { get; set; }
        public event EventHandler<ProgramEventArgs> ProgramEvent;

        public void InvokeProgramEvent(ProgramEventArgs e)
        {
        }
    }
}
