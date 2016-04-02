using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AvalonDock.Layout;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Controls;
using log4net.Config;
using Path = System.IO.Path;

namespace Hawk
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, IMainFrm, IDockableManager
    {
        public Dictionary<string, IXPlugin> PluginDictionary { get; set; }
        public event EventHandler<ProgramEventArgs> ProgramEvent;
        public void InvokeProgramEvent(ProgramEventArgs e)
        {
            
        }

        public MainWindow()
        {
#if !DEBUG
            //     try
            {
#endif
            InitializeComponent();
            MainDescription.MainFrm = this;
            Application.Current.Resources["ThemeDictionary"] = new ResourceDictionary();
            //   this.SetCurrentTheme("ShinyBlue");
        ;
            if (ConfigurationManager.AppSettings["XFrmWork.PluginLocationRelative"] == "true")
            {
                pluginPosition = MainStartUpLocation
                                 + ConfigurationManager.AppSettings["XFrmWork.MainPluginLocation"];
            }
            else
            {
                pluginPosition = ConfigurationManager.AppSettings["XFrmWork.MainPluginLocation"];
            }

            XmlConfigurator.Configure(new FileInfo("log4net.config"));


            string icon = ConfigurationManager.AppSettings["XFrmWork.Icon"];
            Icon = new BitmapImage(new Uri(pluginPosition + icon, UriKind.Absolute));
            PluginManager = new PluginManager();
            //#if !DEBUG
            //Dispatcher.UnhandledException += (s, e) =>
            //{
            //    WPFMessageBox.Show("系统出现异常" + e.Exception);
            //    XLogSys.Print.Fatal(e.Exception);
            //};
            //#endif
            ViewDictionary = new List<ViewItem>();
            Title = ConfigurationManager.AppSettings["XFrmWork.Title"];

            //    this.myDebugSystemUI.MainFrmUI = this;
            PluginManager.MainFrmUI = this;

            //  this.myDebugSystemUI.Init();

            PluginManager.Init(new[] { MainStartUpLocation });
            PluginManager.LoadPlugins();
            PluginManager.LoadView();

            DataContext = this;
            foreach (ICommand action in CommandCollection.Concat(Commands))
            {
                SetCommandKeyBinding(action);
            }
            XLogSys.Print.Info(Title + "已正常启动");


            Closing += (s, e) =>
            {
                if (MessageBox.Show("是否确定离开本软件?", "提示信息", MessageBoxButton.OKCancel) == MessageBoxResult.OK)

                {
                    PluginManager.Close();
                    PluginManager.SaveConfigFile();
                    Process.GetCurrentProcess().Kill();
                }
                else
                {
                    e.Cancel = true;
                }
            };
            //  TestCode();
#if !DEBUG
            }


            // catch (Exception ex)
            {
                //   MessageBox.Show(ex.ToString());
            }
#endif
        }
        public ObservableCollection<IAction> CommandCollection { get; set; }
        private void SetCommandKeyBinding(ICommand command)
        {
            var com = command as Command;
            if (com != null && com.Key != null)
            {
                InputBindings.Add(new KeyBinding
                {
                    Command = com,
                    Key = com.Key.Value,
                    Modifiers = com.Modifiers
                });
            }
            var action = command as IAction;
            if (action != null)
                foreach (ICommand childAction in action.ChildActions)
                {
                    SetCommandKeyBinding(childAction);
                }
        }


        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("数据管理", obj => ActiveThisContent("数据管理")) ,
                        new Command("算法面板", obj => ActiveThisContent("模块管理")) 
                    });
            }
        }

        #region Public Properties
        protected PluginManager PluginManager;
        private string pluginPosition;

        public string MainStartUpLocation => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public  string MainPluginLocation => pluginPosition;

        #endregion
        public List<ViewItem> ViewDictionary { get; private set; }

        public  void ActiveThisContent(string name)
        {
            ViewItem view = ViewDictionary.FirstOrDefault(d => d.Name == name);

            var item = view?.Container as LayoutAnchorable;
            if (item == null)
                return;
            item.Show();
            item.IsActive = true;
        }

        // Private Methods (2) 

        public  void ActiveThisContent(object rc)
        {
            ViewItem view = ViewDictionary.FirstOrDefault(d => d.View == rc);

            var item = view?.Container as LayoutAnchorable;
            if (item == null)
                return;
            item.IsActive = true;
        }

        public  void AddDockAbleContent(FrmState thisState, object thisControl, params string[] objects)
        {
            AddDockAbleContent(thisState, thisControl, objects[0]);
        }

     


        public void RemoveDockableContent(object model)
        {
            ViewItem view2 = ViewDictionary.FirstOrDefault(d => d.Model == model);

            var item = view2?.Container as LayoutAnchorable;
            if (item == null)
                return;
            item.Close();
            ViewDictionary.Remove(view2);
        }

        private LayoutAnchorable Factory(string name, object content)
        {
            var layout = new LayoutAnchorable { Title = name, Content = content };
            layout.Hiding +=
                (s, e) => OnDockManagerUserChanged(new DockChangedEventArgs(DockChangedType.Remove, content));
            layout.Closing +=
                (s, e) => OnDockManagerUserChanged(new DockChangedEventArgs(DockChangedType.Remove, content));
            return layout;
        }
        public event EventHandler<DockChangedEventArgs> DockManagerUserChanged;

        public void OnDockManagerUserChanged(DockChangedEventArgs e)
        {
            EventHandler<DockChangedEventArgs> handler = this.DockManagerUserChanged;
            handler?.Invoke(this, e);
        }
        public  void AddDockAbleContent(FrmState thisState, object thisControl, string name)
        {
            string name2 = null;
            int count = ViewDictionary.Count(d => d.Name == name);
            if (count != 0)
            {
                name2 = name + count;
            }
            else
            {
                name2 = name;
            }
            var viewitem = new ViewItem(thisControl, name, thisState);
            LayoutAnchorable layout = null;
            ViewDictionary.Add(viewitem);
            try
            {
                switch (thisState)
                {
                    case FrmState.Large:
                        layout = Factory(name, thisControl);
                        documentMain.Children.Add(layout);

                        layout.IsActive = true;
                        break;
                    case FrmState.Buttom:
                        layout = Factory(name, thisControl);

                        var view = thisControl as DebugManagerUI;
                        if (view != null)
                        {
                            RichTextBoxAppender.SetRichTextBox(view.richtextBox, DebugText);
                        }

                        documentButtom.Children.Add(layout);

                        layout.IsActive = true;
                        break;
                    case FrmState.Middle:
                        layout = Factory(name, thisControl);
                        viewitem.Container = layout;
                        dockablePane1.Children.Add(layout);

                        layout.IsActive = true;
                        break;
                    case FrmState.Mini:
                        layout = Factory(name, thisControl);
                        viewitem.Container = layout;
                        dockablePane2.Children.Add(layout);

                        layout.IsActive = true;
                        break;
                    case FrmState.Custom:
                        var window = new Window {Title = name};
                        window.Content = thisControl;
                        window.ShowDialog();
                        break;

                    case FrmState.Float:

                        layout = Factory(name, thisControl);

                        documentMain.Children.Add(layout);

                        layout.Float();

                        break;
                }
                viewitem.Container = layout;
            }
            catch (Exception ex)
            {
                XLogSys.Print.Error("加载控件失败," + ex.Message);
            }
        }

        public  void SetBusy(bool isBusyValue, string title = "系统正忙", string message = "正在处理长时间操作",
            int percent = -1)
        {
            BusyIndicator.IsBusy = isBusyValue;
            BusyIndicator.BusyContent = message;

            ProgressBar.Value = 100;
            ProgressBar.IsIndeterminate = isBusyValue;
        }
        private void DebugText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ActiveThisContent("调试信息窗口");
        }

    }
}
