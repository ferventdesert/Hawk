using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Data;
using System.Windows.Input;
using AutoUpdaterDotNET;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
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


        //TODO
        private XFrmWorkPropertyGrid propertyGridWindow;

        private string searchText;

        private IDataProcess selectedProcess;

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
                if (ProjectTaskList.CanFilter)
                {
                    ProjectTaskList.Filter = FilterMethod;
                }
                OnPropertyChanged("SearchText");
            }
        }

        public IDataProcess SelectedProcess
        {
            get { return selectedProcess; }
            set
            {
                if (selectedProcess == value) return;
                selectedProcess = value;

                OnPropertyChanged("SelectedProcess");
            }
        }

        public TaskBase SelectedTask

        {
            get { return _selectedTask; }
            set
            {
                if (_selectedTask == value) return;
                _selectedTask = value;
                OnPropertyChanged("SelectedTask");
                ShowConfigUI(value);
            }
        }

        public ICollection<IDataProcess> CurrentProcessCollections => ProcessCollection;


        public FrmState FrmState => FrmState.Large;


        public object UserControl => null;

        private IDataProcess GetProcess(object data)
        {
            if (data != null) return data as IDataProcess;
            return SelectedProcess ?? null;
        }

        #endregion

        #region Public Methods

        private IDockableManager dockableManager;

        public override bool Close()
        {
            foreach (var currentProcessCollection in CurrentProcessCollections)
            {
                currentProcessCollection.Close();
            }
            return true;
        }

        public void SaveCurrentTasks()
        {
            foreach (var process in CurrentProcessCollections)
            {
                SaveTask(process, false);
            }
            CurrentProject.Save();
        }
        public override bool Init()
        {
            base.Init();
            dockableManager = MainFrmUI as IDockableManager;
            dataManager = MainFrmUI.PluginDictionary["DataManager"] as IDataManager;
            propertyGridWindow = MainFrmUI.PluginDictionary["XFrmWorkPropertyGrid"] as XFrmWorkPropertyGrid;

            var aboutAuthor=new BindingAction(GlobalHelper.Get("key_262"), d =>
            {
                var view = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("key_263"));
                var window = new Window();
                window.Title = GlobalHelper.Get("key_263");
                window.Content = view;
                window.ShowDialog();
            }) {Description = GlobalHelper.Get("key_264"), Icon = "information"};
            var mainlink = new BindingAction(GlobalHelper.Get("key_265"),  d =>
            {
                var url = "https://github.com/ferventdesert/Hawk";
                System.Diagnostics.Process.Start(url);
            }) {Description = GlobalHelper.Get("key_266"),Icon = "home"};
            var helplink = new BindingAction(GlobalHelper.Get("key_267"), d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/wiki";
                System.Diagnostics.Process.Start(url);
            })
            { Description = GlobalHelper.Get("key_268") ,Icon = "question" };

            var feedback = new BindingAction(GlobalHelper.Get("key_269"), d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/issues";
                System.Diagnostics.Process.Start(url);
            })
            { Description = GlobalHelper.Get("key_270") ,Icon = "reply_people"};


            var giveme = new BindingAction(GlobalHelper.Get("key_271"), d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/wiki/8-%E5%85%B3%E4%BA%8E%E4%BD%9C%E8%80%85%E5%92%8C%E6%8D%90%E8%B5%A0";
                System.Diagnostics.Process.Start(url);
            })
            { Description = GlobalHelper.Get("key_272") , Icon = "smiley_happy"};
            var blog = new BindingAction(GlobalHelper.Get("key_273"), d =>
            {
                var url = "http://www.cnblogs.com/buptzym/";
                System.Diagnostics.Process.Start(url);
            }){Description = GlobalHelper.Get("key_274"), Icon = "tower"};

            var update = new BindingAction(GlobalHelper.Get("checkupgrade"), d =>
                {
                    AutoUpdater.Start("https://raw.githubusercontent.com/ferventdesert/Hawk/global/Hawk/autoupdate.xml");

                })
                { Description = GlobalHelper.Get("checkupgrade"), Icon = "arrow_up" };
            var helpCommands = new BindingAction(GlobalHelper.Get("key_275")) {Icon = "magnify"};
            helpCommands.ChildActions.Add(mainlink);
            helpCommands.ChildActions.Add(helplink);
        
            helpCommands.ChildActions.Add(feedback);
            helpCommands.ChildActions.Add(giveme);
            helpCommands.ChildActions.Add(blog);
            helpCommands.ChildActions.Add(aboutAuthor);
            helpCommands.ChildActions.Add(update);
            MainFrmUI.CommandCollection.Add(helpCommands);

            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            var debugCommand= new BindingAction(GlobalHelper.Get("key_276"))
            {
                ChildActions = new ObservableCollection<ICommand>()
                {
                    new BindingAction(GlobalHelper.Get("key_277"))
                    {
                        ChildActions =
                            new ObservableCollection<ICommand>()
                            {
                                new BindingAction("Debug",obj=>hierarchy.Root.Level=Level.Debug),
                                new BindingAction("Info",obj=>hierarchy.Root.Level=Level.Info),
                                new BindingAction("Warn",obj=>hierarchy.Root.Level=Level.Warn),
                                new BindingAction("Error",obj=>hierarchy.Root.Level=Level.Error),
                                new BindingAction("Fatal",obj=>hierarchy.Root.Level=Level.Fatal),
                            }
                    }
                },

                Icon = ""
            };

            MainFrmUI.CommandCollection.Add(debugCommand);
            debugCommand?.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_278"), obj =>
            {

                if (debugGrid == null)
                {
                    debugGrid = PropertyGridFactory.GetInstance(ConfigFile.GetConfig<DataMiningConfig>());
                }
                else
                {
                    debugGrid.SetObjectView(ConfigFile.GetConfig<DataMiningConfig>());
                }
               
                dynamic control =
                    (this.MainFrmUI as IDockableManager).ViewDictionary.FirstOrDefault(d => d.View == debugGrid)
                    ?.Container;
                if (control != null)
                {
                    control.Show();
                }

                else
                {
                    (this.MainFrmUI as IDockableManager).AddDockAbleContent(FrmState.Mini, debugGrid, GlobalHelper.Get("key_278"));
                }


                

            }){Icon = "graph_line"});
            ProcessCollection = new ObservableCollection<IDataProcess>();


            CurrentProcessTasks = new ObservableCollection<TaskBase>();
            BindingCommands = new BindingAction(GlobalHelper.Get("key_279"));
            var sysCommand = new BindingAction();

            sysCommand.ChildActions.Add(
                new Command(
                    GlobalHelper.Get("key_280"),
                    obj =>
                    {
                        if (MessageBox.Show(GlobalHelper.Get("key_281"), GlobalHelper.Get("key_99"), MessageBoxButton.OKCancel) ==
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
                        if (MessageBox.Show(GlobalHelper.Get("key_283"), GlobalHelper.Get("key_99"), MessageBoxButton.OKCancel) ==
                            MessageBoxResult.OK)
                        {
                            SaveCurrentTasks();
                        }
                    }, obj => true,
                    "save"));

            BindingCommands.ChildActions.Add(sysCommand);

            var taskAction1 = new BindingAction();


            taskAction1.ChildActions.Add(new Command(GlobalHelper.Get("key_284"),
                obj => (obj as ProcessTask).Load(true),
                obj => obj is ProcessTask, "inbox_out"));

     
            taskAction1.ChildActions.Add(new Command(GlobalHelper.Get("key_285"),
                obj => CurrentProject.Tasks.Remove(obj as ProcessTask),
                obj => obj is ProcessTask,"delete"));
            taskAction1.ChildActions.Add(new Command(GlobalHelper.Get("key_286"),
             (obj=>(obj as ProcessTask).EvalScript()),
             obj =>(obj is ProcessTask)&& CurrentProcessCollections.FirstOrDefault(d => d.Name == (obj as ProcessTask).Name) != null));
            taskAction1.ChildActions.Add(new Command(GlobalHelper.Get("key_240"),obj=>PropertyGridFactory.GetPropertyWindow(obj).ShowDialog()
            ));



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

            taskAction2.ChildActions.Add(new Command(GlobalHelper.Get("key_289"),
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


            var taskListAction = new BindingAction(GlobalHelper.Get("key_290"));

            taskListAction.ChildActions.Add(new Command(GlobalHelper.Get("key_166"),
                d => CurrentProcessTasks.Execute(d2 => d2.IsSelected = true), null, "check"));

            taskListAction.ChildActions.Add(new Command(GlobalHelper.Get("key_167"),
                d => CurrentProcessTasks.Execute(d2 => d2.IsSelected =!d2.IsSelected), null, "redo"));

            taskListAction.ChildActions.Add(new Command(GlobalHelper.Get("key_291"),
                d => CurrentProcessTasks.Where(d2 => d2.IsSelected).Execute(d2 => d2.IsPause = true), null, "pause"));
            taskListAction.ChildActions.Add(new Command(GlobalHelper.Get("key_292"),
                d => CurrentProcessTasks.Where(d2 => d2.IsSelected).Execute(d2 => d2.IsPause = false), null, "play"));

            taskListAction.ChildActions.Add(new Command(GlobalHelper.Get("key_293"),
               d => CurrentProcessTasks.RemoveElementsNoReturn(d2=>d2.IsSelected,d2=>d2.Remove()), null,"delete"));

            BindingCommands.ChildActions.Add(taskListAction);

            BindingCommands.ChildActions.Add(taskListAction);

            var processAction = new BindingAction();





      var  dataTimer = new System.Windows.Threading.DispatcherTimer();
            var tickInterval = ConfigFile.GetConfig().Get<int>("AutoSaveTime");
            if (tickInterval > 0)
            {
                dataTimer.Tick += new EventHandler(timeCycle);
                dataTimer.Interval = new TimeSpan(0, 0, 0, tickInterval);
                dataTimer.Start();
            }
    

  
    processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_294"), obj =>
            {
                if (obj != null)
                {
                    var process = GetProcess(obj);
                    if (process == null) return;
                    var old = obj as IDataProcess;
                    if (old == null)
                        return;

                    //ProcessCollection.Remove(old);
                    var name = process.GetType().ToString().Split('.').Last();

                    var item = GetOneInstance(name, true, true);
                    (process as IDictionarySerializable).DictCopyTo(item as IDictionarySerializable);
                    item.Init();
                    item.Name = old.Name + "_copy";
                    ProcessCollection.Add(item);

                }
                else
                {
                    var plugin = this.GetOneInstance("SmartETLTool", true, true, true) as SmartETLTool;
                    plugin.Init();
                    ControlExtended.DockableManager.ActiveModelContent(plugin);
                }
              

            }, obj => true, "add"));

            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_295"), obj =>
            {
                if (obj == null)
                {
                    var plugin = this.GetOneInstance("SmartCrawler", true, true, true) as SmartCrawler;
                    plugin.Init();
                    ControlExtended.DockableManager.ActiveModelContent(plugin);
                }
                else
                {
                    var process = GetProcess(obj);
                    if (process == null) return;
                    var old = obj as IDataProcess;
                    if (old == null)
                        return;

                    //ProcessCollection.Remove(old);
                    var name = process.GetType().ToString().Split('.').Last();

                    var item = GetOneInstance(name, true, true);
                    (process as IDictionarySerializable).DictCopyTo(item as IDictionarySerializable);
                    item.Init();
                    item.Name = old.Name + "_copy";
                    ProcessCollection.Add(item);
                }
               
               

            }, obj => true, "cloud_add"));

       

            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_296"), obj =>
            {
                var process = obj as IDataProcess;
                if (process == null)
                {
                    foreach (var target in CurrentProcessCollections)
                    {
                        SaveTask(target, false);
                    
                    }
                }
                else
                {
                    SaveTask(process, true);
                }
              
            }, obj => true,"save"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_297"), obj =>
            {
                var process = GetProcess(obj);
                if (process == null) return;
                var view = (MainFrmUI as IDockableManager).ViewDictionary.FirstOrDefault(d => d.Model == process);
                if (view == null)
                {
                    LoadProcessView(process);
                }
                (MainFrmUI as IDockableManager).ActiveModelContent(process);
                ShowConfigUI(process);
            }, obj => true, "delete"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_298"), obj =>
            {
                var process = GetProcess(obj);
                if (process == null) return;

                RemoveOperation(process);
                ProcessCollection.Remove(process);
                var tasks = this.CurrentProcessTasks.Where(d => d.Publisher == process).ToList();
                if (tasks.Any())
                {
                    foreach (var item in tasks)
                    {
                        item.Remove();
                        XLogSys.Print.Warn(string.Format(GlobalHelper.Get("key_299"),process.Name,item.Name));
                    }

                }
                ShowConfigUI(null);
            }, obj => true, "delete"));
            processAction.ChildActions.Add(new Command(GlobalHelper.Get("key_300"), obj =>
            {
                ControlExtended.DockableManager.ActiveThisContent(GlobalHelper.Get("ModuleMgmt"));
            }, obj => true, "home"));


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
            },icon:"add"));
            BindingCommands.ChildActions.Add(attributeactions);

            var config = ConfigFile.GetConfig<DataMiningConfig>();
            if (config.Projects.Any())
            {
                var project = config.Projects.FirstOrDefault();
                if (project != null)
                {
                    ControlExtended.SafeInvoke(() =>
                    {
                        currentProject = Project.Load(project.SavePath);
                        currentProject.DataCollections.Execute(d=>dataManager.AddDataCollection(d));
                        NotifyCurrentProjectChanged();
                    }, LogType.Info, GlobalHelper.Get("key_303"));
                }
            }

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
                    ((INotifyCollectionChanged) CurrentProcessTasks).CollectionChanged += (s, e) =>
                    {
                        ControlExtended.UIInvoke(() => {
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


    
                OnPropertyChanged("ProjectTaskList");
                ProjectTaskList = new ListCollectionView(CurrentProject.Tasks);
                ProjectTaskList.GroupDescriptions.Clear();

                ProjectTaskList.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
                OnPropertyChanged("ProjectTaskList");
            }

            var fileCommand = MainFrmUI.CommandCollection.FirstOrDefault(d => d.Text == GlobalHelper.Get("key_305"));
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_306"), obj => CreateNewProject()) {Icon = "add"});
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_307"), obj => LoadProject()) {Icon = "inbox_out"});
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_308"), obj => SaveCurrentProject()) {Icon = "save"});
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_309"), obj => SaveCurrentProject(false)) {Icon = "save"});
            fileCommand.ChildActions.Add(new BindingAction(GlobalHelper.Get("key_310"))
            {
                Icon = "save",
                ChildActions =  new ObservableCollection<ICommand>(config.Projects.Select(d=>new BindingAction(d.SavePath, obj => LoadProject(d.SavePath) ) {Icon = "folder"}))
           
            });
            var languageMenu = new BindingAction(GlobalHelper.Get("key_lang")) { Icon = "layout" };

            var files = Directory.GetFiles("Lang");
            foreach (var f in files)
            {
                var ba = new BindingAction(f, obj => { AppHelper.LoadLanguage(f); }) { Icon = "layout" };

                languageMenu.ChildActions.Add(ba);
            }
            helpCommands.ChildActions.Add(languageMenu);


            return true;
        }

        public void SaveTask(IDataProcess process, bool haveui)
        {
            var task = CurrentProject.Tasks.FirstOrDefault(d => d.Name == process.Name);

            if (haveui == false || MessageBox.Show(GlobalHelper.Get("key_311") + (task == null ? GlobalHelper.Get("key_312") : GlobalHelper.Get("key_313")), GlobalHelper.Get("key_99"),
                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                configDocument = (process as IDictionarySerializable).DictSerialize();
                if (task == null)
                {
                    task = new ProcessTask
                    {
                        Name = process.Name,
                        Description = GlobalHelper.Get("key_314"),
                    };

                    CurrentProject.Tasks.Add(task);
                }

                task.ProcessToDo = configDocument;
                XLogSys.Print.Warn(string.Format(GlobalHelper.Get("key_315"),task.Name));
            }
        }

        public ListCollectionView CurrentProcessView { get; set; }
        public ListCollectionView ProcessCollectionView { get; set; }
        public ListCollectionView ProjectTaskList { get; set; }
       private void timeCycle(object sender, EventArgs e)
        {
            SaveCurrentProject(true);
        }

        private void LoadProject(string path=null)
        {
            var project = Project.Load(path);
            if (project != null)
            {
                var config = ConfigFile.GetConfig<DataMiningConfig>();
                config.Projects.RemoveElementsNoReturn(d=>string.IsNullOrWhiteSpace(d.SavePath));
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
                if (project.DataCollections?.Count > 0)
                {//TODO: 添加名称重名？

                    project.DataCollections.Execute(d => dataManager.AddDataCollection(d));
                }
                config.Projects.Insert(0, first);

                currentProject = project;
                NotifyCurrentProjectChanged();
                config.SaveConfig();
            }
        }

        private void SaveCurrentProject(bool isDefaultPosition = true)
        {
            if (currentProject == null)
                return;
            if (CurrentProject.Tasks.Any() == false&& MessageBox.Show(GlobalHelper.Get("key_316"),GlobalHelper.Get("key_151"),MessageBoxButton.OKCancel)==MessageBoxResult.Cancel)
            {
                return;
            }
            if (isDefaultPosition)
            {
                ControlExtended.SafeInvoke(() => currentProject.Save(dataManager.DataCollections), LogType.Important, GlobalHelper.Get("key_317"));
                var pro = ConfigFile.GetConfig<DataMiningConfig>().Projects.FirstOrDefault();
                if (pro != null) pro.SavePath = currentProject.SavePath;
            }
            else
            {
                currentProject.SavePath = null;
                ControlExtended.SafeInvoke(() => currentProject.Save(dataManager.DataCollections), LogType.Important, GlobalHelper.Get("key_318"));
            }
            ConfigFile.Config.SaveConfig();
        }

        private void CreateNewProject()
        {
            var pro = new Project();
            pro.Save();

            var newProj = new ProjectItem();
            pro.DictCopyTo(newProj);

            ConfigFile.GetConfig<DataMiningConfig>().Projects.Insert(0, newProj);
            currentProject = pro;
                var filemanager = new FileManager() { Name = GlobalHelper.Get("key_310") };
                CurrentProject.DBConnections.Add(filemanager);

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
        private TaskBase _selectedTask;
        private WPFPropertyGrid debugGrid;


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
                            this.CurrentProcessCollections.Select(d => d.Name)
                                .Concat(this.CurrentProject.Tasks.Select(d => d.Name));
                        var count = names.Count(d => d.Contains( process.GetType().Name));
                        if (count > 0)
                            process.Name = process.TypeName + count;
                        ProcessCollection.Add(process);
                        XLogSys.Print.Info(GlobalHelper.Get("key_319") + process.TypeName + GlobalHelper.Get("key_320"));
                    }

                    if (isAddUI)
                    {
                        ControlExtended.UIInvoke(() => LoadProcessView(process));
                  
                        ControlExtended.UIInvoke(() => ShowConfigUI(process));
                    }

                    return process;
                }
            }
            return ProcessCollection.Get(name, isAddToList);
        }


        public void RemoveOperation(IDataProcess process)
        {
            dockableManager.RemoveDockableContent(process);
            process.Close();
        }

        public IList<TaskBase> CurrentProcessTasks { get; set; }

        public Project CurrentProject
        {
            get
            {
                if (currentProject == null)
                    currentProject = new Project();
                return currentProject;
            }
            set { currentProject = value; }
        }

        private void NotifyCurrentProjectChanged()
        {
            OnCurrentProjectChanged?.Invoke(this, new EventArgs());
            OnPropertyChanged("CurrentProject");


            ProjectTaskList = new ListCollectionView(CurrentProject.Tasks);
            ProjectTaskList.GroupDescriptions.Clear();

            ProjectTaskList.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
            OnPropertyChanged("ProjectTaskList");
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
            var view = PluginManager.AddCusomView(MainFrmUI as IDockableManager, rc.GetType().Name, rc as IView,rc.Name);
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
            XLogSys.Print.Info(GlobalHelper.Get("key_321"));
        }

        private void ShowConfigUI(object method)
        {
            propertyGridWindow?.SetObjectView(method);
        }

        #endregion
        
    }


    public class ProcessGroupConverter:IValueConverter
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