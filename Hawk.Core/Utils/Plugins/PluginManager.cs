using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 插件管理器
    /// </summary>
    public class PluginManager
    {
        #region Constants and Fields





        #endregion




        #region Constructors and Destructors

        public PluginManager()
        {
            this.PluginFolders = new List<string>();


        }

        #endregion

        #region Properties

     

        /// <summary>
        /// 文件命令菜单
        /// </summary>
        public BindingAction FileCommands => new BindingAction("文件")
        {
            ChildActions =
                new ObservableCollection<ICommand> (),Icon = "disk"
        };

        public FrmState FrmState => FrmState.Mini;

        public IMainFrm MainFrmUI { get; set; }

        /// <summary>
        /// 系统插件字典
        /// </summary>
        public Dictionary<Type, List<XFrmWorkAttribute>> SystemPluginDictionary => PluginProvider.PluginDictionary;


        public object PluginNames => PluginLoadControllor.Instance.PluginNames;
        private List<string> PluginFolders { get; set; }

        #endregion

        #region Public Methods

        public bool Close()
        {
            //this.SaveConfigFile();
            this.ReleaseAllPlugin();
            return true;
        }

        public bool Init(IEnumerable<string> pluginFolders)
        {
            this.PluginFolders = pluginFolders.ToList();
            XLogSys.Print.Info("插件管理器已加载");
            this.MainFrmUI.CommandCollection = new ObservableCollection<IAction> { this.FileCommands  };

            this.MainFrmUI.PluginDictionary = new Dictionary<string, IXPlugin>();

            PluginProvider.OrderedSearchFolder = this.PluginFolders.ToArray();
            PluginProvider.MainConfigFolder = this.MainFrmUI.MainPluginLocation;
            PluginProvider.GetAllPluginName(false);

            PluginProvider.GetAllPlugins(false);
            List<XFrmWorkAttribute> plugins = PluginProvider.GetPluginCollection(typeof(IXPlugin));
            if (PluginLoadControllor.IsNormalLoaded == false)
            {
                PluginLoadControllor.Instance.AddBuildLogic<IXPlugin>(plugins);
                PluginProvider.SaveConfigFile();
            }
      

            XLogSys.Print.Info("开始对插件字典中的插件进行初始化");
            //var pluginCommands = new BindingAction("插件");
            //foreach (XFrmWorkAttribute plugin in PluginProvider.GetPluginCollection(typeof(IXPlugin)))
            //{
            //    var action = new BindingAction(plugin.Name);


            //    action.ChildActions = new ObservableCollection<ICommand> {
            //            new Command( "加载",  obj => this.AddNewPlugin(plugin),
            //                        obj => (!this.MainFrmUI.PluginDictionary.ContainsKey(plugin.Name))),
            //                         new Command( "卸载", obj=>this.ReleasePlugin(action.Text),
            //                        obj => this.MainFrmUI.PluginDictionary.ContainsKey(plugin.Name)),


            //        };
            //    pluginCommands.ChildActions.Add(action);
            //}
            //pluginCommands.ChildActions.Add(new Command("显示插件列表",obj=>
            //    {
            //        var view2 = PluginProvider.GetObjectInstance<ICustomView>("插件管理器");
            //        var frameworkElement = view2 as FrameworkElement;
            //        if (frameworkElement != null)
            //        {
            //            frameworkElement.DataContext = this;
            //        }
            //        var dockableManager = this.MainFrmUI as IDockableManager;
            //        dockableManager?.AddDockAbleContent(view2.FrmState,view2,"系统插件信息");
            //    }));
            //   this.MainFrmUI.CommandCollection.Add(pluginCommands);

            return true;
        }

        /// <summary>
        /// 加载优先级在指定范围内的插件
        /// </summary>
        /// <returns></returns>
        public bool LoadPlugins(int minPriority = 1, int maxPriority = 99)
        {
            IList<IXPlugin> plugins = new List<IXPlugin>();
            PluginProvider.LoadOrderedPlugins<IXPlugin>(
                d => plugins.Add(this.AddNewPlugin(d)), minPriority, maxPriority);
            foreach (IXPlugin xPlugin in plugins)
            {
                xPlugin?.Init();
            }
            return true;
        }

        public void LoadView()
        {
            IEnumerable<IXPlugin> orderdPlugins = from plugin in this.MainFrmUI.PluginDictionary
                                                  let order =
                                                      PluginLoadControllor.Instance.GetOrder<IXPlugin>(plugin.Key)
                                                  orderby order descending
                                                  select plugin.Value;

            foreach (IXPlugin plugin in orderdPlugins)
            {
                var rc3 = plugin as IMainFrmMenu;
                if (rc3 != null &&rc3.BindingCommands!=null)
                {
                    this.MainFrmUI.CommandCollection.Add(rc3.BindingCommands);
                }
                var ui = plugin as IView;
                if (ui != null)
                {
                   var control=  AddCusomView(this.MainFrmUI as IDockableManager,plugin.TypeName,ui,plugin.Name);
                    if (control is UserControl)
                    {
                        (control as UserControl).DataContext = ui;
                    }
                }
            }
        }

        /// <summary>
        ///     向UI窗口里插入一个可智能查询的View
        /// </summary>
        /// <param name="dockableManager"></param>
        /// <param name="plugin"></param>
        /// <param name="model"></param>
        /// <param name="bindingAction"></param>
        public  static object AddCusomView(IDockableManager dockableManager, string pluginName, IView model,string name)
        {
            if (dockableManager == null || model == null)
                return null;
            object view1 = model.UserControl;
            FrmState frm;
            frm = model.FrmState;
           
            XFrmWorkAttribute first = PluginProvider.GetFirstPlugin(typeof(ICustomView), pluginName);

            if (first != null)
            {
                var view2 = PluginProvider.GetObjectInstance(first.MyType) as ICustomView;
                if (view2 != null)
                {
                    view1 = view2;
                    frm = view2.FrmState;
                }
            }
            if (view1 == null)
                return null;
            XFrmWorkAttribute attr = PluginProvider.GetPluginAttribute(model.GetType());

            dockableManager.AddDockAbleContent(
                frm, view1, new[] { name, attr.LogoURL, attr.Description });
            dockableManager.ViewDictionary.Last().Model = model;
            return view1;
        }

        /// <summary>
        /// 释放所有插件
        /// </summary>
        public void ReleaseAllPlugin()
        {
            IEnumerable<IXPlugin> orderdPlugins = from plugin in this.MainFrmUI.PluginDictionary
                                                  let order =
                                                      PluginLoadControllor.Instance.GetOrder<IXPlugin>(plugin.Key)
                                                  orderby order descending
                                                  select plugin.Value;
            foreach (var rc in orderdPlugins)
            {
                rc.Close();
            }
        }

        public void SaveConfigFile()
        {
            foreach (var rc in this.MainFrmUI.PluginDictionary)
            {
            
                rc.Value.SaveConfigFile();
            }
            if(!PluginLoadControllor.IsNormalLoaded)
            PluginProvider.SaveConfigFile();
        }

        #endregion

        #region Methods

        private IXPlugin AddNewPlugin(XFrmWorkAttribute describe)
        {
            if (this.MainFrmUI.PluginDictionary.ContainsKey(describe.Name))
            {

              XLogSys.Print.Error($"插件类型{describe.Name}发现重复，请检查配置文件");
                return null;
            }
            var plugin = PluginProvider.GetObjectInstance(describe.MyType) as IXPlugin;
         
            this.MainFrmUI.PluginDictionary.Add(describe.Name, plugin);

            if (plugin != null)
            {
                plugin.MainFrmUI = this.MainFrmUI;
            }

            XLogSys.Print.Info(plugin.TypeName + "已成功初始化");

            return plugin;
        }

        private void ReleasePlugin(string name)
        {
            IXPlugin plugin = null;
            if (!this.MainFrmUI.PluginDictionary.TryGetValue(name, out plugin))
            {
                return;
            }
            if (MessageBox.Show("在运行时卸载插件可能造成程序崩溃,确定继续？", "警告信息", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
                MessageBoxResult.Yes)
            {
                return;
            }
            plugin.SaveConfigFile();
            plugin.Close();
            var view = plugin as IView;
            if (view == null)
            {
                return;
            }
            var dockableManager = this.MainFrmUI as IDockableManager;
            if (dockableManager != null)
            {
                dockableManager.RemoveDockableContent(view);
            }

            MainFrmUI.PluginDictionary.Remove(name);
        }

        #endregion
    }
}