using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AutoUpdaterDotNET;
using AvalonDock.Layout;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Controls;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using log4net.Config;
using Microsoft.WindowsAPICodePack.Taskbar;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

namespace Hawk
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary
    public partial class MainWindow : Window, IMainFrm, IDockableManager, INotifyPropertyChanged
    {
        private readonly Notifier notifier;
        private readonly TaskbarManager windowsTaskbar = TaskbarManager.Instance;

        public MainWindow()
        {
#if !DEBUG
    //     try
            {
#endif
            InitializeComponent();
            MainDescription.MainFrm = this;


            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(Application.Current.MainWindow, Corner.TopRight, 10,
                    10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    TimeSpan.FromSeconds(2),
                    MaximumNotificationCount.FromCount(3));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof (DependencyObject), new FrameworkPropertyMetadata(60000));
            Application.Current.Resources["ThemeDictionary"] = new ResourceDictionary();
            //   this.SetCurrentTheme("ShinyBlue");
            ;
            if (ConfigurationManager.AppSettings["PluginLocationRelative"] == "true")
            {
                MainPluginLocation = MainStartUpLocation
                                     + ConfigurationManager.AppSettings["MainPluginLocation"];
            }
            else
            {
                MainPluginLocation = ConfigurationManager.AppSettings["MainPluginLocation"];
            }

            XmlConfigurator.Configure(new FileInfo("log4net.config"));

            var icon = ConfigurationManager.AppSettings["Icon"];
            try
            {
                Icon = new BitmapImage(new Uri(MainPluginLocation + icon, UriKind.Absolute));
            }
            catch (Exception)
            {
                XLogSys.Print.Error(GlobalHelper.Get("IconNotExist"));
            }

            PluginManager = new PluginManager();
#if !DEBUG
            Dispatcher.UnhandledException += (s, e) =>
            {

                if (MessageBox.Show(GlobalHelper.Get("key_0"), GlobalHelper.Get("key_1"), MessageBoxButton.YesNoCancel) ==
                    MessageBoxResult.Yes)
                {
                    dynamic process = PluginDictionary["DataProcessManager"];
                    process.SaveCurrentTasks();
                }

                MessageBox.Show(GlobalHelper.Get("key_2") + e.Exception);
                XLogSys.Print.Fatal(e.Exception);
            };
#endif
            AppHelper.LoadLanguage();
            ViewDictionary = new List<ViewItem>();
            Title = ConfigurationManager.AppSettings["Title"];

            //    this.myDebugSystemUI.MainFrmUI = this;
            PluginManager.MainFrmUI = this;

            //  this.myDebugSystemUI.Init();
            PluginManager.Init(new[] {MainStartUpLocation});


            PluginManager.LoadPlugins();
            PluginManager.LoadView();

            DataContext = this;
            foreach (var action in CommandCollection.Concat(Commands))
            {
                SetCommandKeyBinding(action);
            }
            XLogSys.Print.Info(Title + GlobalHelper.Get("Start"));


            AutoUpdater.Start("https://raw.githubusercontent.com/ferventdesert/Hawk/master/Hawk/autoupdate.xml");
            Closing += (s, e) =>
            {
                List<IDataProcess> revisedTasks;
                var processmanager = PluginDictionary["DataProcessManager"] as DataProcessManager;
                revisedTasks = processmanager.GetRevisedTasks().ToList();
                if (!revisedTasks.Any())
                {
                    if (
                        MessageBox.Show(GlobalHelper.Get("Closing"), GlobalHelper.Get("Tips"), MessageBoxButton.OKCancel) ==
                        MessageBoxResult.OK)
                    {
                        PluginManager.Close();
                        PluginManager.SaveConfigFile();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    var result =
                        MessageBox.Show(GlobalHelper.FormatArgs(
                            "RemaindSave", " ".Join(revisedTasks.Select(d => d.Name).ToArray())),
                            GlobalHelper.Get("Tips"), MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes || result == MessageBoxResult.No)
                    {
                        if (result == MessageBoxResult.Yes)
                        {
                            revisedTasks.Execute(d => processmanager.SaveTask(d, false));
                        }
                        PluginManager.Close();
                        PluginManager.SaveConfigFile();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            };
            //File.WriteAllText("helper.md", ETLHelper.GetTotalMarkdownDoc());
            // var md = ETLHelper.GetAllToolMarkdownDoc();
            // File.WriteAllText("HawkDoc.md", md);
            //  TestCode();
#if !DEBUG
            }


            // catch (Exception ex)
            {
                //   MessageBox.Show(ex.ToString());
            }
#endif
        }

        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("DataMgmt"), obj => ActiveThisContent(GlobalHelper.Get("DataMgmt"))),
                        new Command(GlobalHelper.Get("ModuleMgmt"),
                            obj => ActiveThisContent(GlobalHelper.Get("ModuleMgmt")))
                    });
            }
        }

        public List<ViewItem> ViewDictionary { get; }

        public void ActiveThisContent(string name)
        {
            var view = ViewDictionary.FirstOrDefault(d => d.Name == name);

            var item = view?.Container as LayoutAnchorable;
            if (item == null)
                return;
            item.Show();
            item.IsActive = true;
        }

        // Private Methods (2) 

        public void ActiveThisContent(object rc)
        {
            var view = ViewDictionary.FirstOrDefault(d => d.View == rc);

            var item = view?.Container as LayoutAnchorable;
            if (item == null)
                return;
            if (item.IsHidden)
                item.Show();
            item.IsActive = true;
        }

        public void AddDockAbleContent(FrmState thisState, object thisControl, params string[] objects)
        {
            AddDockAbleContent(thisState, thisControl, objects[0]);
        }

        public void RemoveDockableContent(object model)
        {
            var view2 = ViewDictionary.FirstOrDefault(d => d.Model == model);

            var item = view2?.Container as LayoutAnchorable;
            if (item == null)
                return;
            item.Close();
            ViewDictionary.Remove(view2);
        }

        public event EventHandler<DockChangedEventArgs> DockManagerUserChanged;

        public void SetBusy(ProgressBarState state = ProgressBarState.Normal, string title = null, string message = null,
            int percent = -1)
        {
            if (title == null)
                title = GlobalHelper.Get("key_3");
            if (message == null)
                message = GlobalHelper.Get("LongTask");
            BusyIndicator.IsBusy = state != ProgressBarState.NoProgress;
           BusyIndicator.DisplayAfter= TimeSpan.FromSeconds(1);
            BusyIndicator.BusyContent = message;
         
        

            if (state == ProgressBarState.Normal)
            {
                windowsTaskbar.SetProgressValue(percent, 100, this);
                ProgressBar.Value = percent;

            }
            else
            {
                ProgressBar.IsIndeterminate = state == ProgressBarState.Indeterminate;
                windowsTaskbar.SetProgressState((TaskbarProgressBarState)(state), this);
            }
           
        }

        public Dictionary<string, IXPlugin> PluginDictionary { get; set; }
        public event EventHandler<ProgramEventArgs> ProgramEvent;

        public void InvokeProgramEvent(ProgramEventArgs e)
        {
        }

        public ObservableCollection<IAction> CommandCollection { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

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
                foreach (var childAction in action.ChildActions)
                {
                    SetCommandKeyBinding(childAction);
                }
        }


        private LayoutAnchorable Factory(string name, object content)
        {
            var layout = new LayoutAnchorable {Title = name, Content = content};
            layout.Hiding +=
                (s, e) => OnDockManagerUserChanged(new DockChangedEventArgs(DockChangedType.Remove, content));
            layout.Closing +=
                (s, e) => OnDockManagerUserChanged(new DockChangedEventArgs(DockChangedType.Remove, content));
            if (name == GlobalHelper.Get("DataProcessManager_name"))
            {
                layout.CanClose = false;
                layout.CanHide = false;
            }
            return layout;
        }

        public void OnDockManagerUserChanged(DockChangedEventArgs e)
        {
            var handler = DockManagerUserChanged;
            handler?.Invoke(this, e);
        }

        public void AddDockAbleContent(FrmState thisState, object thisControl, string name)
        {
            string name2 = null;
            var count = ViewDictionary.Count(d => d.Name == name);
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
                            RichTextBoxAppender.SetRichTextBox(view.richtextBox, DebugText, notifier);
                        }

                        documentButtom.Children.Add(layout);
                        documentButtom.Children.RemoveElementsNoReturn(d => d.Content == null);
                        layout.IsActive = true;
                        layout.CanClose = false;
                        break;
                    case FrmState.Middle:
                        layout = Factory(name, thisControl);
                        viewitem.Container = layout;
                        dockablePane1.Children.Add(layout);
                        dockablePane1.Children.RemoveElementsNoReturn(d => d.Content == null);
                        layout.IsActive = true;
                        layout.CanClose = false;
                        break;
                    case FrmState.Mini:
                        layout = Factory(name, thisControl);
                        viewitem.Container = layout;
                        dockablePane2.Children.Add(layout);
                        dockablePane2.Children.RemoveElementsNoReturn(d => d.Content == null);
                        layout.IsActive = true;
                        break;
                    case FrmState.Mini2:
                        layout = Factory(name, thisControl);
                        viewitem.Container = layout;
                        dockablePane3.Children.Add(layout);
                        dockablePane3.Children.RemoveElementsNoReturn(d => d.Content == null);
                        layout.IsActive = true;
                        layout.CanClose = false;
                        break;
                    case FrmState.Custom:
                        var window = new Window {Title = name};
                        window.Content = thisControl;
                        window.ShowDialog();
                        break;

                    case FrmState.Float:

                        layout = Factory(name, thisControl);

                        dockablePane1.Children.Add(layout);

                        layout.Float();

                        break;
                }
                var canNotClose = new[]
                {GlobalHelper.Get("ModuleMgmt"), GlobalHelper.Get("SysState"), GlobalHelper.Get("DebugView")};
                if (canNotClose.Contains(name))
                    if (layout != null) layout.CanClose = false;
                viewitem.Container = layout;
            }
            catch (Exception ex)
            {
                XLogSys.Print.ErrorFormat("{0}{1},{2}", GlobalHelper.Get("ControlLoad"), GlobalHelper.Get("Error"),
                    ex.Message);
            }
        }

        private void DebugText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ActiveThisContent(GlobalHelper.Get("key_4"));
        }

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Public Properties

        protected PluginManager PluginManager;

        public string MainStartUpLocation => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public string MainPluginLocation { get; }

        #endregion
    }
}