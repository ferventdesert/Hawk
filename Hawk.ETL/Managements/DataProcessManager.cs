using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using AutoUpdaterDotNET;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Market;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Plugins.Transformers;
using Hawk.ETL.Process;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace Hawk.ETL.Managements
{
    [XFrmWork("DataProcessManager_name", "DataProcessManager_desc", "")]
    public class DataProcessManager : AbstractPlugIn, IProcessManager, IView
    {
        private FreeDocument configDocument;

        #region Constants and Fields

        private IDataManager dataManager;

        public WPFPropertyGrid ProjectPropertyWindow => PropertyGridFactory.GetInstance(CurrentProject);

        public WPFPropertyGrid SystemConfigWindow => PropertyGridFactory.GetInstance(ConfigFile.GetConfig());

        private string searchText;

        #endregion

        #region Events

        #endregion
    
        #region Properties

        public IAction BindingCommands { get; private set; }


        public ObservableCollection<IDataProcess> ProcessCollection { get; set; }

        public ListCollectionView ProgramNameFilterView { get; set; }

        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (searchText == value) return;
                searchText = value;
                if (MarketProjectList.CanFilter)
                {
                    MarketProjectList.Filter = FilterMethod;
                }
                OnPropertyChanged("SearchText");
            }
        }


        public ProjectItem SelectedRemoteProject

        {
            get { return _selectedRemoteProject; }
            set
            {
                if (_selectedRemoteProject == value) return;
                _selectedRemoteProject = value;
                OnPropertyChanged("SelectedRemoteProject");
                OnPropertyChanged("SelectedLocalProject");
            }
        }

        private int mainTabIndex;
        public int MainTabIndex 

        {
            get { return mainTabIndex; }
            set
            {
                if (mainTabIndex == value) return;
                mainTabIndex = value;
                OnPropertyChanged("MainTabIndex");
            }
        }

        public Project SelectedLocalProject

        {
            get
            {
                //return null;
                return GetRemoteProjectContent().Result;
            }
        }

        private readonly Dictionary<ProjectItem, Project> RemoteProjectBuff = new Dictionary<ProjectItem, Project>();

        public async Task<Project> GetRemoteProjectContent(ProjectItem projectItem=null)

        {
            if (projectItem == null)
                projectItem = SelectedRemoteProject;
            Project project = null;
            if (projectItem == null || projectItem.IsRemote == false)
                return null;
            ControlExtended.SetBusy(ProgressBarState.NoProgress);
            Monitor.Enter(RemoteProjectBuff);
            if (RemoteProjectBuff.TryGetValue(projectItem, out project))
            {
                Monitor.Exit(RemoteProjectBuff);
                return project;
            }
            Monitor.Exit(RemoteProjectBuff);
            ControlExtended.SetBusy(ProgressBarState.Indeterminate, message:GlobalHelper.Get("get_remote_market_data"));
            project = await Project.LoadFromUrl(projectItem.SavePath);
            ControlExtended.SetBusy(ProgressBarState.NoProgress);
            Monitor.Enter(RemoteProjectBuff);
            if (RemoteProjectBuff.ContainsKey(projectItem))
            {
                RemoteProjectBuff[projectItem] = project;
            }
            else
            {
                RemoteProjectBuff.Add(projectItem, project);
            }
            Monitor.Exit(RemoteProjectBuff);
            project.DictDeserialize(projectItem.DictSerialize());

            return project;
        }


        public ICollection<IDataProcess> CurrentProcessCollections => ProcessCollection;
        private ListBox processView;
        private ListView currentProcessTasksView;
        public FrmState FrmState => FrmState.Large;


        public object UserControl => null;

        private IEnumerable<IDataProcess> GetSelectedProcess(object data)
        {
            if (data != null)
            {
                yield return data as IDataProcess;
                yield break;
            }
            if (processView == null)
                yield break;


            foreach (var item in processView.SelectedItems.IListConvert<IDataProcess>())

                yield return item;
        }

        public ObservableCollection<ProjectItem> MarketProjects { get; set; }

        private IEnumerable<TaskBase> GetSelectedTask(object data)
        {
            if (data != null)
            {
                yield return data as TaskBase;
                yield break;
            }
            if (processView == null)
                yield break;
            foreach (var item in currentProcessTasksView.SelectedItems.IListConvert<TaskBase>())

                yield return item;
        }

        #endregion

        #region Public Methods

        public GitHubAPI GitHubApi { get;private set; }

        private IDockableManager dockableManager;

        public override bool Close()
        {
            foreach (var currentProcessCollection in CurrentProcessCollections)
            {
                currentProcessCollection.Close();
            }
            return true;
        }

                private bool NeedSave()
        {
            if (this.CurrentProcessCollections.Any() || this.dataManager.DataCollections.Any())
            {
                return true;
            }
            return false;
        }
        private DispatcherTimer datatTimer;
        public override bool Init()
        {
            base.Init();
            GitHubApi = new GitHubAPI();
            MarketProjects = new ObservableCollection<ProjectItem>();
            dockableManager = MainFrmUI as IDockableManager;
            dataManager = MainFrmUI.PluginDictionary["DataManager"] as IDataManager;
            ProcessCollection = new ObservableCollection<IDataProcess>();
            CurrentProcessTasks = new ObservableCollection<TaskBase>();
            if (!MainDescription.IsUIForm)
            {
                return true;
            }
            this.datatTimer = new DispatcherTimer();
            var aboutAuthor = new BindingAction(GlobalHelper.Get("key_262"), d =>
            {
                var view = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("key_263"));
                var window = new Window();
                window.Title = GlobalHelper.Get("key_263");
                window.Content = view;
                window.ShowDialog();
            }) {Description = GlobalHelper.Get("key_264"), Icon = "information"};
            var mainlink = new BindingAction(GlobalHelper.Get("key_265"), d =>
            {
                var url = "https://github.com/ferventdesert/Hawk";
                System.Diagnostics.Process.Start(url);
            }) {Description = GlobalHelper.Get("key_266"), Icon = "home"};
            var helplink = new BindingAction(GlobalHelper.Get("key_267"), d =>
            {
                var url = "https://ferventdesert.github.io/Hawk/";
                System.Diagnostics.Process.Start(url);
            })
            {Description = GlobalHelper.Get("key_268"), Icon = "question"};

            var feedback = new BindingAction(GlobalHelper.Get("key_269"), d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/issues";
                System.Diagnostics.Process.Start(url);
            })
            {Description = GlobalHelper.Get("key_270"), Icon = "reply_people"};


            var giveme = new BindingAction(GlobalHelper.Get("key_271"), d =>
            {
                var url =
                    "https://github.com/ferventdesert/Hawk/wiki/8-%E5%85%B3%E4%BA%8E%E4%BD%9C%E8%80%85%E5%92%8C%E6%8D%90%E8%B5%A0";
                System.Diagnostics.Process.Start(url);
            })
            {Description = GlobalHelper.Get("key_272"), Icon = "smiley_happy"};
            var blog = new BindingAction(GlobalHelper.Get("key_273"), d =>
            {
                var url = "http://www.cnblogs.com/buptzym/";
                System.Diagnostics.Process.Start(url);
            }) {Description = GlobalHelper.Get("key_274"), Icon = "tower"};

            var update = new BindingAction(GlobalHelper.Get("checkupgrade"),
                d =>
                {
                    AutoUpdater.Start("https://raw.githubusercontent.com/ferventdesert/Hawk/global/Hawk/autoupdate.xml");
                })
            {Description = GlobalHelper.Get("checkupgrade"), Icon = "arrow_up"};
            var helpCommands = new BindingAction(GlobalHelper.Get("key_275")) {Icon = "magnify"};
            helpCommands.ChildActions.Add(mainlink);
            helpCommands.ChildActions.Add(helplink);

            helpCommands.ChildActions.Add(feedback);
            helpCommands.ChildActions.Add(giveme);
            helpCommands.ChildActions.Add(blog);
            helpCommands.ChildActions.Add(aboutAuthor);
            helpCommands.ChildActions.Add(update);
            MainFrmUI.CommandCollection.Add(helpCommands);

            var hierarchy = (Hierarchy) LogManager.GetRepository();
            var debugCommand = new BindingAction(GlobalHelper.Get("debug"))
            {
                ChildActions = new ObservableCollection<ICommand>
                {
                    new BindingAction(GlobalHelper.Get("key_277"))
                    {
                        ChildActions =
                            new ObservableCollection<ICommand>
                            {
                                new BindingAction("Debug", obj => hierarchy.Root.Level = Level.Debug),
                                new BindingAction("Info", obj => hierarchy.Root.Level = Level.Info),
                                new BindingAction("Warn", obj => hierarchy.Root.Level = Level.Warn),
                                new BindingAction("Error", obj => hierarchy.Root.Level = Level.Error),
                                new BindingAction("Fatal", obj => hierarchy.Root.Level = Level.Fatal)
                            }
                    }
                },
                Icon = ""
            };

            MainFrmUI.CommandCollection.Add(debugCommand);
         
            BindingCommands = new BindingAction(GlobalHelper.Get("key_279"));
            var sysCommand = new BindingAction();

            sysCommand.ChildActions.Add(
                new Command(
                    GlobalHelper.Get("key_280"),
                    obj =>
                    {
                        if (
                            MessageBox.Show(GlobalHelper.Get("key_281"), GlobalHelper.Get("key_99"),
                                MessageBoxButton.OKCancel) ==
                            MessageBoxResult.OK)
                        {
                            ProcessCollection.RemoveElementsNoReturn(d => true, RemoveOperation);
                        }
                    }, obj => true,
                    "clear"));

            sysCommand.ChildActions.Add(
                new Command(
                    GlobalHelper.Get("key_282"),
                    obj =>
                    {
                        if (
                            MessageBox.Show(GlobalHelper.Get("key_283"), GlobalHelper.Get("key_99"),
                                MessageBoxButton.OKCancel) ==
                            MessageBoxResult.OK)
                        {
                            SaveCurrentProject();
                        }
                    }, obj => true,
                    "save"));

            BindingCommands.ChildActions.Add(sysCommand);

            var taskAction1 = new BindingAction();


            taskAction1.ChildActions.Add(new Command(GlobalHelper.Get("key_284"),
                async obj =>
                {
                    var project = await GetRemoteProjectContent();
                    if (project != null)
                    {
                        foreach (var param in project.Parameters)
                        {
                            //TODO: how check if it is same? name?
                            if (CurrentProject.Parameters.FirstOrDefault(d => d.Name == param.Name) == null)
                                CurrentProject.Parameters.Add(param);
                        }
                        CurrentProject.ConfigSelector.SelectItem = project.ConfigSelector.SelectItem;
                    }

                    (obj as ProcessTask).Load(true);
                },
                obj => obj is ProcessTask, "inbox_out"));


         


            BindingCommands.ChildActions.Add(taskAction1);
            var taskAction2 = new BindingAction(GlobalHelper.Get("key_287"));
            taskAction2.ChildActions.Add(new Command(GlobalHelper.Get("key_288"),
                obj =>
                {
                    var task = obj as TaskBase;
                    task.Start();
                },
                obj =>
                {
                    var task = obj as TaskBase;
                    return task != null && task.IsStart == false;
                }, "play"));

            taskAction2.ChildActions.Add(new Command(GlobalHelper.Get("cancel_task"),
                obj =>
                {
                    var task = obj as TaskBase;
                    if (task.IsStart)
                    {
                        task.Remove();
                    }

                    task.Remove();
                },
                obj =>
                {
                    var task = obj as TaskBase;
                    return task != null;
                }, "cancel"));


            var runningTaskActions = new BindingAction(GlobalHelper.Get("key_290"));


            runningTaskActions.ChildActions.Add(new Command(GlobalHelper.Get("key_291"),
                d => GetSelectedTask(d).Execute(d2 => { d2.IsPause = true; d2.ShouldPause = true; }), d=>true, "pause"));
            runningTaskActions.ChildActions.Add(new Command(GlobalHelper.Get("key_292"),
                d => GetSelectedTask(d).Execute(d2 => { d2.IsPause = false;d2.ShouldPause = false; }), d=> ProcessTaskCanExecute(d,false), "play"));

            runningTaskActions.ChildActions.Add(new Command(GlobalHelper.Get("key_293"),
                d =>
                {
                    var selectedTasks = GetSelectedTask(d).ToList();
                    CurrentProcessTasks.Where(d2 => selectedTasks.Contains(d2)).ToList().Execute( d2 => d2.Remove());
                }, d => ProcessTaskCanExecute(d, null), "delete"));


            runningTaskActions.ChildActions.Add(new Command(GlobalHelper.Get("property"),
            d =>
            {
                var selectedTasks = GetSelectedTask(d).FirstOrDefault();
                PropertyGridFactory.GetPropertyWindow(selectedTasks).ShowDialog();

            }, d => ProcessTaskCanExecute(d, null), "settings"));



            BindingCommands.ChildActions.Add(runningTaskActions);
            BindingCommands.ChildActions.Add(runningTaskActions);


            var processAction = new BindingAction();

          
            dynamic processview =
                (MainFrmUI as IDockableManager).ViewDictionary.FirstOrDefault(d => d.Name == GlobalHelper.Get("key_794"))
                    .View;
            processView = processview.processListBox as ListBox;

            var tickInterval = ConfigFile.GetConfig().Get<int>("AutoSaveTime");
            if (tickInterval > 0)
            {


                this.datatTimer.Tick += timeCycle;
                this.datatTimer.Interval = new TimeSpan(0, 0, 0, tickInterval);
                this.datatTimer.Start();
            }
            ConfigFile.GetConfig().PropertyChanged += (s, e) =>
            {
                if(e.PropertyName== "AutoSaveTime")
                {
                    var tick = ConfigFile.GetConfig().Get<int>("AutoSaveTime");
                    if (tick <= 0)
                        tick = 1000000;               
                    this.datatTimer.Interval = new TimeSpan(0, 0, 0, tick);

                }
            };

            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_294"), obj =>
            {
                if (obj != null)
                {
                    foreach (var process in GetSelectedProcess(obj))
                    {
                        if (process == null) return;
                        var old = obj as IDataProcess;
                        if (old == null)
                            return;

                        var name = process.GetType().ToString().Split('.').Last();
                        var item = GetOneInstance(name, true, true);
                        (process as IDictionarySerializable).DictCopyTo(item as IDictionarySerializable);
                        item.Init();
                        item.Name = process.Name + "_copy";
                    }
                }
                else
                {
                    var plugin = GetOneInstance("SmartETLTool", true, true, true) as SmartETLTool;
                    plugin.Init();
                    ControlExtended.DockableManager.ActiveModelContent(plugin);
                }
            }, obj => true, "add"));

            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_295"), obj =>
            {
                if (obj == null)
                {
                    var plugin = GetOneInstance("SmartCrawler", true, true, true) as SmartCrawler;
                    plugin.Init();
                    ControlExtended.DockableManager.ActiveModelContent(plugin);
                }
                else
                {
                    foreach (var process in GetSelectedProcess(obj))
                    {
                        if (process == null) return;
                        var name = process.GetType().ToString().Split('.').Last();
                        var item = GetOneInstance(name, true, true);

                        (process as IDictionarySerializable).DictCopyTo(item as IDictionarySerializable);
                        item.Init();
                        item.Name = process.Name + "_copy";
                    }
                }
            }, obj => true, "cloud_add"));


            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_296"), obj =>
            {
                if (obj == null)
                {
                    SaveCurrentProject();
                }
                else
                {
                    foreach (var process in GetSelectedProcess(obj))
                    {
                        SaveTask(process, false);
                    }
                }
            }, obj => true, "save"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_297"), obj =>
            {
                var process = GetSelectedProcess(obj).FirstOrDefault();
                if (process == null) return;
                var view = (MainFrmUI as IDockableManager).ViewDictionary.FirstOrDefault(d => d.Model == process);
                if (view == null)
                {
                    LoadProcessView(process);
                }
                (MainFrmUI as IDockableManager).ActiveModelContent(process);
             
                process.Init();
            }, obj => true, "tv"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_298"), obj =>
            {
                if (MessageBox.Show(GlobalHelper.Get("delete_confirm"),GlobalHelper.Get("key_99"),MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    return;
                }
                foreach (var process in GetSelectedProcess(obj))
                {
                    if (process == null) return;

                    RemoveOperation(process);
                    ProcessCollection.Remove(process);
                    var tasks = CurrentProcessTasks.Where(d => d.Publisher == process).ToList();
                    if (tasks.Any())
                    {
                        foreach (var item in tasks)
                        {
                            item.Remove();
                            XLogSys.Print.Warn(string.Format(GlobalHelper.Get("key_299"), process.Name, item.Name));
                        }
                    }
                }
            }, obj => true, "delete"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_300"),
                obj => { ControlExtended.DockableManager.ActiveThisContent(GlobalHelper.Get("ModuleMgmt")); },
                obj => true, "home"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("find_ref"),
                obj =>
                {
                    PrintReferenced(obj as IDataProcess);

                },obj=>true, "diagram"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("property"),
              obj =>
              {
                  PropertyGridFactory.GetPropertyWindow(obj).ShowDialog();

              }, obj => true, "settings"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("param_group"),
          obj =>
          {
              this.dockableManager.ActiveThisContent(GlobalHelper.Get("ModuleMgmt"));
              MainTabIndex = 2;
              

          }, obj => true, "equalizer"));
            BindingCommands.ChildActions.Add(processAction);
            BindingCommands.ChildActions.Add(taskAction2);


            var attributeactions = new BindingAction(GlobalHelper.Get("key_301"));
            attributeactions.ChildActions.Add(new Command(GlobalHelper.Get("key_302"), obj =>
            {
                var attr = obj as XFrmWorkAttribute;
                if (attr == null)
                    return;

                var process = GetOneInstance(attr.MyType.Name, newOne: true, isAddUI: true);
                process.Init();
            }, icon: "add"));
            BindingCommands.ChildActions.Add(attributeactions);


            var marketAction = new BindingAction();
            marketAction.ChildActions.Add(new Command(GlobalHelper.Get("connect_market"), async obj =>
            {
                GitHubApi.Connect(ConfigFile.GetConfig().Get<string>("Login"), ConfigFile.GetConfig().Get<string>("Password"));
                MarketProjects.Clear();
                ControlExtended.SetBusy(ProgressBarState.Indeterminate,message:GlobalHelper.Get("get_remote_projects"));
                MarketProjects.AddRange(await GitHubApi.GetProjects(ConfigFile.GetConfig().Get<string>("MarketUrl")));
                ControlExtended.SetBusy(ProgressBarState.NoProgress);

            }, icon: "refresh"));
          
            BindingCommands.ChildActions.Add(marketAction);


           var  marketProjectAction=new BindingAction();

            marketProjectAction.ChildActions.Add(new Command(GlobalHelper.Get("key_307"),async obj =>
            {

                var projectItem=obj as ProjectItem;
                var keep = MessageBoxResult.Yes;
                if(projectItem==null)
                    return;
                if (MessageBox.Show(GlobalHelper.Get("is_load_remote_project"), GlobalHelper.Get("key_99"),MessageBoxButton.OKCancel)==MessageBoxResult.Cancel)
                {
                    return;
                }
                if (NeedSave())
                {
                    keep = MessageBox.Show(GlobalHelper.Get("keep_old_datas"), GlobalHelper.Get("key_99"),
                        MessageBoxButton.YesNoCancel);
                    if (keep == MessageBoxResult.Cancel)
                        return;
                }
                var proj =await this.GetRemoteProjectContent(projectItem);
                LoadProject(proj,keep == MessageBoxResult.Yes);
            }, icon: "download"));


            var config = ConfigFile.GetConfig<DataMiningConfig>();
            if (config.Projects.Any())
            {
                var project = config.Projects.FirstOrDefault();
                if (project != null)
                {
                    ControlExtended.SafeInvoke(() => { CurrentProject = LoadProject(project.SavePath); }, LogType.Info,
                        GlobalHelper.Get("key_303"));
                }
            }
            BindingCommands.ChildActions.Add(marketProjectAction);
            if (MainDescription.IsUIForm)
            {
                ProgramNameFilterView =
                    new ListCollectionView(PluginProvider.GetPluginCollection(typeof (IDataProcess)).ToList());

                ProgramNameFilterView.GroupDescriptions.Clear();
                ProgramNameFilterView.GroupDescriptions.Add(new PropertyGroupDescription("GroupName"));
                var taskView = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("key_304"));
                var userControl = taskView as UserControl;
                if (userControl != null)
                {
                    userControl.DataContext = this;
                    dynamic control = userControl;
                    currentProcessTasksView = control.currentProcessTasksView;
                    ((INotifyCollectionChanged) CurrentProcessTasks).CollectionChanged += (s, e) =>
                    {
                        ControlExtended.UIInvoke(() =>
                        {
                            if (e.Action == NotifyCollectionChangedAction.Add)
                            {
                                dockableManager.ActiveThisContent(GlobalHelper.Get("key_304"));
                            }
                        });
                    }
                        ;
                    dockableManager.AddDockAbleContent(taskView.FrmState, this, taskView, GlobalHelper.Get("key_304"));
                }
                ProcessCollectionView = new ListCollectionView(ProcessCollection);
                ProcessCollectionView.GroupDescriptions.Clear();
                ProcessCollectionView.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));


               
            }

            var fileCommand = MainFrmUI.CommandCollection.FirstOrDefault(d => d.Text == GlobalHelper.Get("key_305"));
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_306"), obj => CreateNewProject())
            {
                Icon = "add"
            });
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_307"), obj =>
            {
                var keep= MessageBoxResult.No;
                if (NeedSave())
                {
                    keep = MessageBox.Show(GlobalHelper.Get("keep_old_datas"), GlobalHelper.Get("key_99"),
                        MessageBoxButton.YesNoCancel);
                    if (keep == MessageBoxResult.Cancel)
                        return;
                }
                LoadProject(keepLast: keep == MessageBoxResult.Yes);
            }) {Icon = "inbox_out"});
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_308"), obj => SaveCurrentProject())
            {
                Icon = "save"
            });
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_309"), obj => SaveCurrentProject(false))
            {
                Icon = "save"
            });
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("generate_project_doc"), obj =>
            {
                if(CurrentProject==null)
                    return;
                var doc = this.GenerateRemark(this.ProcessCollection);
                var docItem = new DocumentItem() { Title = this.CurrentProject.Name, Document = doc };
                PropertyGridFactory.GetPropertyWindow(docItem).ShowDialog();
            },obj=>CurrentProject!=null)
            {
                Icon = "help"
            });
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("recent_file"))
            {
                Icon = "save",
                ChildActions =
                    new ObservableCollection<ICommand>(config.Projects.Select(d => new BindingAction(d.SavePath, obj =>
                    {
                        var keep = MessageBoxResult.No;
                        if (NeedSave())
                        {
                            keep = MessageBox.Show(GlobalHelper.Get("keep_old_datas"), GlobalHelper.Get("key_99"),
                                MessageBoxButton.YesNoCancel);
                            if (keep == MessageBoxResult.Cancel)
                                return;
                        }
                        LoadProject(d.SavePath, keep == MessageBoxResult.Yes);
                    }) {Icon = "folder"}))
            });
            var languageMenu = new BindingAction(GlobalHelper.Get("key_lang")) {Icon = "layout"};

            var files = Directory.GetFiles("Lang");
            foreach (var f in files)
            {
                var ba = new BindingAction(f, obj => { AppHelper.LoadLanguage(f); }) {Icon = "layout"};

                languageMenu.ChildActions.Add(ba);
            }
            //  helpCommands.ChildActions.Add(languageMenu);

            return true;
        }

        public void SaveTask(IDataProcess process, bool haveui)
        {
            var task = CurrentProject.Tasks.FirstOrDefault(d => d.Name == process.Name);

            if (haveui == false ||
                MessageBox.Show(
                    GlobalHelper.Get("key_311") +
                    (task == null ? GlobalHelper.Get("key_312") : GlobalHelper.Get("key_313")),
                    GlobalHelper.Get("key_99"),
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                configDocument = (process as IDictionarySerializable).DictSerialize();
                if (task == null)
                {
                    task = new ProcessTask
                    {
                        Name = process.Name,
                        Description = GlobalHelper.Get("key_314")
                    };

                    CurrentProject.Tasks.Add(task);
                }

                task.ProcessToDo = configDocument;
                // XLogSys.Print.Warn(string.Format(GlobalHelper.Get("key_315"),task.Name));
            }
        }

        private bool ProcessTaskCanExecute(object source,bool? shouldPause)
        {
            var tasks = GetSelectedTask(source).ToList();
            if (tasks.Any()==false)
                return false;
            if (shouldPause == null)
                return true;
            if (tasks.TrueForAll(d => d.IsPause == shouldPause))
                return false;
            return true;

        }
        /// <summary>
        /// 查找引用该模块的所有任务
        /// </summary>
        /// <param name="obj"></param>
        private void PrintReferenced(IDataProcess obj)
        {
            if (obj is SmartETLTool)
            {
                var tool = obj as SmartETLTool;
                var oldtools = CurrentProcessCollections.OfType<SmartETLTool>().SelectMany(d => d.CurrentETLTools).OfType<ETLBase>().Where(d => d.ETLSelector.SelectItem == tool.Name).ToList();
                XLogSys.Print.Info("===================="+GlobalHelper.Get("smartetl_name")+"===================");
                foreach (var oldtool in oldtools)
                {
                    XLogSys.Print.Info(string.Format("{0} :{1}", oldtool.Father.Name, oldtool.ObjectID));
                }

            }
            if (obj is SmartCrawler)
            {
                var tool = obj as SmartCrawler;
                var _name = tool.Name;
                var oldCrawler = CurrentProcessCollections.OfType<SmartCrawler>()
               .Where(d => d.ShareCookie.SelectItem == _name).ToList();
                var oldEtls = CurrentProcessCollections.OfType<SmartETLTool>()
                    .SelectMany(d => d.CurrentETLTools).OfType<ResponseTF>()
                    .Where(d => d.CrawlerSelector.SelectItem == _name).ToList();
                XLogSys.Print.Info("====================" + GlobalHelper.Get("smartcrawler_name")+"=================");
                
                foreach (var oldtool in oldCrawler)
                {
                    XLogSys.Print.Info(string.Format("\t{0}", oldtool.Name));
                }
                XLogSys.Print.Info(GlobalHelper.Get("smartetl_name"));
                foreach (var oldtool in oldEtls)
                {
                    XLogSys.Print.Info(string.Format("{0} :{1}", oldtool.Father.Name, oldtool.ObjectID));
                }

            }
            XLogSys.Print.Info("======================================================================");
        }
        public ListCollectionView CurrentProcessView { get; set; }
        public ListCollectionView ProcessCollectionView { get; set; }
        private ListCollectionView marketCollectionView;
        public ListCollectionView MarketProjectList {
            get
            {
                if (marketCollectionView == null)
                {
                    GitHubApi.Connect(ConfigFile.GetConfig().Get<string>("Login"), ConfigFile.GetConfig().Get<string>("Password"));
                    var result = GitHubApi.GetProjects(ConfigFile.GetConfig().Get<string>("MarketUrl")).Result;
                    ControlExtended.SafeInvoke(
                        () =>
                        {
                            MarketProjects.Clear();
                            MarketProjects.AddRange(result);
                            marketCollectionView = new ListCollectionView(MarketProjects);
                        }
                    ,LogType.Info, GlobalHelper.Get("market_login"),true);
                 
                 
                }
                return marketCollectionView;
            }
        }

        private void timeCycle(object sender, EventArgs e)
        {


            if (NeedSave())
            {
                dynamic welcomeWindow =  PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("auto_save_tooltip")) ;
                
                welcomeWindow.ShowDialogAdvance();

                if (welcomeWindow.DialogResult==true)
                {
                    SaveCurrentProject(true);
                }
            }
        } 

        private void SetWindowTitleName(string name)
        {
         
            if (MainDescription.IsUIForm)
            {
                var window = MainFrmUI as Window;
                if (window != null)
                {
                    var originTitle = ConfigurationManager.AppSettings["Title"];
                    if (originTitle == null)
                        originTitle = "";
                    window.Title = name + " - " + originTitle;
                }
            }
        }
        private Project LoadProject(string path = null, bool keepLast = false)
        {
            var project = Project.Load(path);
            return LoadProject(project, keepLast);
        }

        private Project LoadProject(Project project, bool keepLast = false)
        {
            if (project != null)
            {
                var config = ConfigFile.GetConfig<DataMiningConfig>();
                config.Projects.RemoveElementsNoReturn(d => string.IsNullOrWhiteSpace(d.SavePath));
                var first = config.Projects.FirstOrDefault(d => d.SavePath == project.SavePath);
                if (first != null)
                {
                    config.Projects.Remove(first);
                }
                else
                {
                    first = new ProjectItem();
                    project.DictCopyTo(first);
                }
                if (!keepLast)
                {
                    CleanAllItems();

                }
                dataManager.LoadDataConnections(project.DBConnections);
                if (project.DataCollections?.Count > 0)
                {
                    //TODO: 添加名称重名？

                    project.DataCollections.Execute(d => dataManager.AddDataCollection(d));
                }
                config.Projects.Insert(0, first);
                CurrentProject = project;
                var name = Path.GetFileNameWithoutExtension(project.SavePath);
                if (string.IsNullOrEmpty(CurrentProject.Name))
                {
                    CurrentProject.Name = name;
                }

                foreach (var task in project.Tasks)
                {
                    task.Load(false);
                }
                if (string.IsNullOrEmpty(project.ConfigSelector.SelectItem) == false)
                    this.CurrentProject.ConfigSelector.SelectItem = project.ConfigSelector.SelectItem;
                else
                {
                    this.CurrentProject.ConfigSelector.SetDefault();
                }
                NotifyCurrentProjectChanged();
                config.SaveConfig();
                project.LoadRunningTasks();
            }
            return project;

        }
        private void SaveCurrentProject(bool isDefaultPosition = true)
        {
            if (CurrentProject == null)
                return;
            CurrentProject.Tasks.Clear();
            foreach (var process in CurrentProcessCollections)
            {
                SaveTask(process, false);
            }
            if (CurrentProject.Tasks.Any() == false &&
                MessageBox.Show(GlobalHelper.Get("key_316"), GlobalHelper.Get("key_151"), MessageBoxButton.OKCancel) ==
                MessageBoxResult.Cancel)
            {
                return;
            }
         
            if (isDefaultPosition)
            {
                Task.Factory.StartNew(() =>
                {
                    ControlExtended.SetBusy(ProgressBarState.Indeterminate, message: GlobalHelper.Get("key_308"));
                    ControlExtended.SafeInvoke(() =>
                    {
                        CurrentProject.Save(dataManager.DataCollections);
                    }, LogType.Important,
                        GlobalHelper.Get("key_317"));
                    var pro = ConfigFile.GetConfig<DataMiningConfig>().Projects.FirstOrDefault();
                    if (pro != null) pro.SavePath = CurrentProject.SavePath;
                    ControlExtended.SetBusy(ProgressBarState.NoProgress);
                });
            }
            else
            {
                CurrentProject.SavePath = null;
                ControlExtended.SafeInvoke(() => CurrentProject.Save(dataManager.DataCollections), LogType.Important,
                    GlobalHelper.Get("key_318"));
            }
            ConfigFile.Config.SaveConfig();
        }

        private void CleanAllItems()
        {
            this.CurrentProcessTasks.RemoveElementsNoReturn(d=>true,d=>d.Remove());
            this.dataManager.CurrentConnectors.Clear();
            this.dataManager.DataCollections.Clear();
            ProcessCollection.RemoveElementsNoReturn(d => true, RemoveOperation);
        }
        public void CreateNewProject()
        {
            var project = new Project();
            project.Save();

            var newProj = new ProjectItem();
            project.DictCopyTo(newProj);

            ConfigFile.GetConfig<DataMiningConfig>().Projects.Insert(0, newProj);
            CurrentProject = project;
            CleanAllItems();
            var filemanager = new FileManager {Name = GlobalHelper.Get("recent_file")};
            dataManager.CurrentConnectors.Add(filemanager);

            NotifyCurrentProjectChanged();
        }

        public override void SaveConfigFile()
        {
            CurrentProject?.Save(dataManager.DataCollections);

            ConfigFile.GetConfig().SaveConfig();
        }

        public IEnumerable<IDataProcess> GetRevisedTasks()
        {
            foreach (var process in CurrentProcessCollections.OfType<AbstractProcessMethod>())
            {
                var task = CurrentProject.Tasks.FirstOrDefault(d => d.Name == process.Name);
                if (task == null)
                {
                    yield return process;
                    continue;
                }
                if (!task.ProcessToDo.IsEqual(process.DictSerialize()))
                {
                    yield return process;
                }
            }
        }

        #endregion

        #region Implemented Interfaces

        #region IProcessManager

        private Project currentProject;
        private WPFPropertyGrid debugGrid;
        private ProjectItem _selectedRemoteProject;


        public IDataProcess GetOneInstance(string name, bool isAddToList = true, bool newOne = false,
            bool isAddUI = false)
        {
            if (newOne)
            {
                var process = PluginProvider.GetObjectByType<IDataProcess>(name);
                if (process != null)
                {
                    if (isAddToList)
                    {
                        ;
                        process.SysDataManager = dataManager;

                        process.SysProcessManager = this;
                        var rc4 = process as AbstractProcessMethod;
                        if (rc4 != null)
                        {
                            rc4.MainPluginLocation = MainFrmUI.MainPluginLocation;
                            rc4.MainFrm = MainFrmUI;
                        }
                        var names =
                            CurrentProcessCollections.Select(d => d.Name);
                        var count = names.Count(d => d.Contains(process.TypeName));
                        if (count > 0)
                            process.Name = process.TypeName + (count+1);
                        CurrentProcessCollections.Add(process);
                        XLogSys.Print.Info(GlobalHelper.Get("key_319") + process.TypeName + GlobalHelper.Get("key_320"));
                    }

                    if (isAddUI)
                    {
                        ControlExtended.UIInvoke(() => LoadProcessView(process));
                    }

                    return process;
                }
            }
            return ProcessCollection.Get(name, isAddToList);
        }

        public T GetTask<T>(string name) where T : class, IDataProcess
        {
            var module = CurrentProcessCollections.OfType<T>().FirstOrDefault(d => d.Name == name);
            if (module != null)
                return module;
            return null;
            var project = GetRemoteProjectContent().Result;
            if (project != null)
            {
                var task = project.Tasks.FirstOrDefault(d => d.TaskType == typeof (T).Name && d.Name == name);
                var newtask = task?.Load(false);
                return newtask as T;
            }
            return null;
        }

        public DataCollection GetCollection(string name)
        {
            var collection = dataManager.DataCollections.FirstOrDefault(d => d.Name == name);
            if (collection != null)
                return collection;
            var project = GetRemoteProjectContent().Result;
            if (project != null)
            {
                collection = project.DataCollections.FirstOrDefault(d => d.Name == name);
                return collection;
            }
            return null;
        }


        public void RemoveOperation(IDataProcess process)
        {
            dockableManager.RemoveDockableContent(process);
            process.Close();
        }

        public IList<TaskBase> CurrentProcessTasks { get; set; }

        public int TaskRunningPercent
        {
            get
            {
                if (CurrentProcessTasks.Count == 0)
                    return 0;
                return CurrentProcessTasks[0].Percent;
            }
        }

        public Project CurrentProject
        {
            get
            {
                if (currentProject == null)
                    currentProject = new Project();
                return currentProject;
            }
            set
            {
                if (currentProject != value)
                {
                    currentProject = value;
                    OnPropertyChanged("CurrentProject");
                    OnPropertyChanged("ProjectPropertyWindow");
                    SetWindowTitleName(CurrentProject.Name);
                    CurrentProject.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "Name")
                        {
                            SetWindowTitleName(CurrentProject.Name);
                        }
                    };
                }
            }
        }

        private void NotifyCurrentProjectChanged()
        {
            OnCurrentProjectChanged?.Invoke(this, new EventArgs());
            OnPropertyChanged("CurrentProject");
        }

        public event EventHandler OnCurrentProjectChanged;

        #endregion

        #region IViewManager

        #endregion

        #endregion

        #region Methods

        private bool FilterMethod(object obj)
        {
            var process = obj as TaskBase;
            if (process == null)
            {
                return false;
            }
            if (process.Name.Contains(SearchText) || process.Description.Contains(SearchText))
            {
                return true;
            }
            return false;
        }


        private void LoadProcessView(IDataProcess rc)
        {
            var view = PluginManager.AddCusomView(MainFrmUI as IDockableManager, rc.GetType().Name, rc as IView, rc.Name);
            var control = view as UserControl;
            if (control != null)
            {
                control.DataContext = rc;
            }
        }


        private void SetCurrentTask(ProcessTask task)
        {
            var process = ProcessCollection.FirstOrDefault(d => d.Name == task.Name);
            if (process == null)
                return;
            var configDocument = (process as IDictionarySerializable).DictSerialize();
            task.ProcessToDo = configDocument;
            XLogSys.Print.Info(GlobalHelper.Get("cover_task_succ"));
        }


        #endregion
    }


    public class ProcessGroupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "haha";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}