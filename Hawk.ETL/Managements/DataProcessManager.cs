using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Data;
using System.Windows.Input;
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

  
    [XFrmWork("模块管理", "对算法模块实现管理和组装，但不提供界面", "")]
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
            dataManager = MainFrmUI.PluginDictionary["数据管理"] as IDataManager;
            propertyGridWindow = MainFrmUI.PluginDictionary["属性配置器"] as XFrmWorkPropertyGrid;

            var aboutAuthor=new BindingAction("关于", d =>
            {
                var view = PluginProvider.GetObjectInstance<ICustomView>("关于作者");
                var window = new Window();
                window.Title = "关于作者";
                window.Content = view;
                window.ShowDialog();
            }) {Description = "Hawk版本与检查更新", Icon = "information"};
            var mainlink = new BindingAction("项目主页",  d =>
            {
                var url = "https://github.com/ferventdesert/Hawk";
                System.Diagnostics.Process.Start(url);
            }) {Description = "访问Hawk的开源项目地址",Icon = "home"};
            var helplink = new BindingAction("使用文档", d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/wiki";
                System.Diagnostics.Process.Start(url);
            })
            { Description = "这里有使用Hawk的案例与完整教程" ,Icon = "question" };

            var feedback = new BindingAction("反馈问题", d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/issues";
                System.Diagnostics.Process.Start(url);
            })
            { Description = "出现bug或者问题了？欢迎反馈" ,Icon = "reply_people"};


            var giveme = new BindingAction("捐赠", d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/wiki/8-%E5%85%B3%E4%BA%8E%E4%BD%9C%E8%80%85%E5%92%8C%E6%8D%90%E8%B5%A0";
                System.Diagnostics.Process.Start(url);
            })
            { Description = "你的支持是作者更新Hawk的动力" , Icon = "smiley_happy"};
            var blog = new BindingAction("博客", d =>
            {
                var url = "http://www.cnblogs.com/buptzym/";
                System.Diagnostics.Process.Start(url);
            }){Description = "作者沙漠君的博客", Icon = "tower"};
         
            var helpCommands = new BindingAction("帮助") {Icon = "magnify"};
            helpCommands.ChildActions.Add(mainlink);
            helpCommands.ChildActions.Add(helplink);
        
            helpCommands.ChildActions.Add(feedback);
            helpCommands.ChildActions.Add(giveme);
            helpCommands.ChildActions.Add(blog);
            helpCommands.ChildActions.Add(aboutAuthor);
            MainFrmUI.CommandCollection.Add(helpCommands);

            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            var debugCommand= new BindingAction("调试")
            {
                ChildActions = new ObservableCollection<ICommand>()
                {
                    new BindingAction("级别设置")
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
            debugCommand?.ChildActions.Add(new BindingAction("Web请求统计", obj =>
            {

                if (debugGrid == null)
                {
                    debugGrid = PropertyGridFactory.GetInstance(RequestManager.Instance);
                }
                else
                {
                    debugGrid.SetObjectView(RequestManager.Instance);
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
                    (this.MainFrmUI as IDockableManager).AddDockAbleContent(FrmState.Mini, debugGrid, "Web请求统计");
                }


                

            }){Icon = "graph_line"});
            ProcessCollection = new ObservableCollection<IDataProcess>();


            CurrentProcessTasks = new ObservableCollection<TaskBase>();
            BindingCommands = new BindingAction("运行");
            var sysCommand = new BindingAction();

            sysCommand.ChildActions.Add(
                new Command(
                    "清空任务列表",
                    obj =>
                    {
                        if (MessageBox.Show("确定清空所有算法模块么？", "提示信息", MessageBoxButton.OKCancel) ==
                            MessageBoxResult.OK)
                        {
                            ProcessCollection.RemoveElementsNoReturn(d => true, RemoveOperation);
                        }
                    }, obj => true,
                    "clear"));

            sysCommand.ChildActions.Add(
                new Command(
                    "保存全部任务",
                    obj =>
                    {
                        if (MessageBox.Show("确定保存所有算法模块么？", "提示信息", MessageBoxButton.OKCancel) ==
                            MessageBoxResult.OK)
                        {
                            SaveCurrentTasks();
                        }
                    }, obj => true,
                    "save"));

            BindingCommands.ChildActions.Add(sysCommand);

            var taskAction1 = new BindingAction();


            taskAction1.ChildActions.Add(new Command("加载本任务",
                obj => (obj as ProcessTask).Load(true),
                obj => obj is ProcessTask, "inbox_out"));

     
            taskAction1.ChildActions.Add(new Command("删除任务",
                obj => CurrentProject.Tasks.Remove(obj as ProcessTask),
                obj => obj is ProcessTask,"delete"));
            taskAction1.ChildActions.Add(new Command("执行任务脚本",
             (obj=>(obj as ProcessTask).EvalScript()),
             obj =>(obj is ProcessTask)&& CurrentProcessCollections.FirstOrDefault(d => d.Name == (obj as ProcessTask).Name) != null));
            taskAction1.ChildActions.Add(new Command("配置",obj=>PropertyGridFactory.GetPropertyWindow(obj).ShowDialog()
            ));



            BindingCommands.ChildActions.Add(taskAction1);
            var taskAction2 = new BindingAction("任务列表2");
            taskAction2.ChildActions.Add(new Command("开始任务",
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

            taskAction2.ChildActions.Add(new Command("取消任务",
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


            var taskListAction = new BindingAction("任务列表命令");

            taskListAction.ChildActions.Add(new Command("全选",
                d => CurrentProcessTasks.Execute(d2 => d2.IsSelected = true), null, "check"));

            taskListAction.ChildActions.Add(new Command("反选",
                d => CurrentProcessTasks.Execute(d2 => d2.IsSelected =!d2.IsSelected), null, "redo"));

            taskListAction.ChildActions.Add(new Command("暂停",
                d => CurrentProcessTasks.Where(d2 => d2.IsSelected).Execute(d2 => d2.IsPause = true), null, "pause"));
            taskListAction.ChildActions.Add(new Command("恢复",
                d => CurrentProcessTasks.Where(d2 => d2.IsSelected).Execute(d2 => d2.IsPause = false), null, "play"));

            taskListAction.ChildActions.Add(new Command("取消",
               d => CurrentProcessTasks.RemoveElementsNoReturn(d2=>d2.IsSelected,d2=>d2.Remove()), null,"delete"));

            BindingCommands.ChildActions.Add(taskListAction);

            BindingCommands.ChildActions.Add(taskListAction);

            var processAction = new BindingAction();






            processAction.ChildActions.Add(new Command("新建或复制数据清洗", obj =>
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

            processAction.ChildActions.Add(new Command("新建或复制采集器", obj =>
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

       

            processAction.ChildActions.Add(new Command("保存任务", obj =>
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
            processAction.ChildActions.Add(new Command("显示并配置", obj =>
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
            processAction.ChildActions.Add(new Command("移除", obj =>
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
                        XLogSys.Print.Warn($"由于任务{process.Name} 已经被删除， 相关任务${item.Name} 也已经被强行取消");
                    }

                }
                ShowConfigUI(null);
            }, obj => true, "delete"));
            processAction.ChildActions.Add(new Command("打开欢迎页面", obj =>
            {
                ControlExtended.DockableManager.ActiveThisContent("模块管理");
            }, obj => true, "home"));


            BindingCommands.ChildActions.Add(processAction);
            BindingCommands.ChildActions.Add(taskAction2);
            var attributeactions = new BindingAction("模块");
            attributeactions.ChildActions.Add(new Command("添加", obj =>
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
                        currentProject = ProjectItem.LoadProject(project.SavePath);
                        NotifyCurrentProjectChanged();
                    }, LogType.Info, "加载默认工程");
                }
            }

            if (MainDescription.IsUIForm)
            {
                ProgramNameFilterView =
                    new ListCollectionView(PluginProvider.GetPluginCollection(typeof (IDataProcess)).ToList());

                ProgramNameFilterView.GroupDescriptions.Clear();
                             ProgramNameFilterView.GroupDescriptions.Add(new PropertyGroupDescription("GroupName"));
                var taskView = PluginProvider.GetObjectInstance<ICustomView>("工作线程视图");
                var userControl = taskView as UserControl;
                if (userControl != null)
                {
                    userControl.DataContext = this;
                    ((INotifyCollectionChanged) CurrentProcessTasks).CollectionChanged += (s, e) =>
                    {
                        ControlExtended.UIInvoke(() => {
                            if (e.Action == NotifyCollectionChangedAction.Add)
                            {
                                dockableManager.ActiveThisContent("工作线程视图");
                            }
                        });
                     
                    }
                        ;
                    dockableManager.AddDockAbleContent(taskView.FrmState, this, taskView, "工作线程视图");
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

            var file = MainFrmUI.CommandCollection.FirstOrDefault(d => d.Text == "文件");
            file.ChildActions.Add(new BindingAction("新建项目", obj => CreateNewProject()) {Icon = "add"});
            file.ChildActions.Add(new BindingAction("加载项目", obj => LoadProject()) {Icon = "inbox_out"});
            file.ChildActions.Add(new BindingAction("保存项目", obj => SaveCurrentProject()) {Icon = "save"});
            file.ChildActions.Add(new BindingAction("项目另存为", obj => SaveCurrentProject(false)) {Icon = "save"});
            file.ChildActions.Add(new BindingAction("最近打开的文件")
            {
                Icon = "save",
                ChildActions =  new ObservableCollection<ICommand>(config.Projects.Select(d=>new BindingAction(d.SavePath, obj => LoadProject(d.SavePath) ) {Icon = "folder"}))
           
            });
            return true;
        }

        public void SaveTask(IDataProcess process, bool haveui)
        {
            var task = CurrentProject.Tasks.FirstOrDefault(d => d.Name == process.Name);

            if (haveui == false || MessageBox.Show("是否确定保存任务?" + (task == null ? "将新建任务" : "存在同名任务，将覆盖该任务"), "提示信息",
                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                configDocument = (process as IDictionarySerializable).DictSerialize();
                if (task == null)
                {
                    task = new ProcessTask
                    {
                        Name = process.Name,
                        Description = "任务描述",
                    };

                    CurrentProject.Tasks.Add(task);
                }

                task.ProcessToDo = configDocument;
                XLogSys.Print.Warn($"任务 {task.Name} 已经成功保存");
            }
        }

        public ListCollectionView CurrentProcessView { get; set; }
        public ListCollectionView ProcessCollectionView { get; set; }
        public ListCollectionView ProjectTaskList { get; set; }

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
            if (CurrentProject.Tasks.Any() == false&& MessageBox.Show("当前工程中没有包含任何任务，请在任务管理页中，将要保存的任务插入到当前工程中","警告信息",MessageBoxButton.OKCancel)==MessageBoxResult.Cancel)
            {
                return;
            }
            if (isDefaultPosition)
            {
                ControlExtended.SafeInvoke(() => currentProject.Save(), LogType.Important, "保存当前工程");
                var pro = ConfigFile.GetConfig<DataMiningConfig>().Projects.FirstOrDefault();
                if (pro != null) pro.SavePath = currentProject.SavePath;
            }
            else
            {
                currentProject.SavePath = null;
                ControlExtended.SafeInvoke(() => currentProject.Save(), LogType.Important, "另存为当前工程");
            }
            ConfigFile.Config.SaveConfig();
        }

        private void CreateNewProject()
        {
            var pro = new Project();
            pro.Save();

            var probase = new ProjectItem();
            pro.DictCopyTo(probase);
            ConfigFile.GetConfig<DataMiningConfig>().Projects.Insert(0, probase);
            currentProject = pro;
            NotifyCurrentProjectChanged();
        }

        public override void SaveConfigFile()
        {
            CurrentProject?.Save();

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
                        var count = names.Count(d => d.Contains( process.TypeName));
                        if (count > 0)
                            process.Name = process.TypeName + count;
                        ProcessCollection.Add(process);
                        XLogSys.Print.Info("已经成功添加" + process.TypeName + "到当前列表");
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
            var view = PluginManager.AddCusomView(MainFrmUI as IDockableManager, rc.TypeName, rc as IView,rc.Name);
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
            XLogSys.Print.Info("已经成功覆盖任务");
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