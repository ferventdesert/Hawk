using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

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

            var aboutAuthor=new BindingAction("联系和打赏作者", d =>
            {
                var view = PluginProvider.GetObjectInstance<ICustomView>("关于作者");
                var window = new Window();
                window.Title = "关于作者";
                window.Content = view;
                window.ShowDialog();
            });
            var helplink = new BindingAction("使用文档", d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/wiki";
                System.Diagnostics.Process.Start(url);
            });

            var feedback = new BindingAction("反馈问题", d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/issues";
                System.Diagnostics.Process.Start(url);
            });


            var giveme = new BindingAction("捐赠", d =>
            {
                var url = "https://github.com/ferventdesert/Hawk/wiki/8.%E5%85%B3%E4%BA%8E%E4%BD%9C%E8%80%85";
                System.Diagnostics.Process.Start(url);
            });

            var pluginCommands = new BindingAction("帮助");
            pluginCommands.ChildActions.Add(helplink);
            pluginCommands.ChildActions.Add(aboutAuthor);
            pluginCommands.ChildActions.Add(feedback);
            pluginCommands.ChildActions.Add(giveme);
            MainFrmUI.CommandCollection.Add(pluginCommands);
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
                    "clear"));

            BindingCommands.ChildActions.Add(sysCommand);

            var taskAction1 = new BindingAction();


            taskAction1.ChildActions.Add(new Command("加载本任务",
                obj => (obj as ProcessTask).Load(true),
                obj => obj is ProcessTask, "download"));

     
            taskAction1.ChildActions.Add(new Command("删除任务",
                obj => CurrentProject.Tasks.Remove(obj as ProcessTask),
                obj => obj is ProcessTask));
            taskAction1.ChildActions.Add(new Command("执行任务脚本",
             (obj=>(obj as ProcessTask).EvalScript()),
             obj =>(obj is ProcessTask)&& CurrentProcessCollections.FirstOrDefault(d => d.Name == (obj as ProcessTask).Name) != null));

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
                }, "download"));

            taskAction2.ChildActions.Add(new Command("取消任务",
                obj =>
                {
                    var task = obj as TaskBase;
                    if (task.IsStart)
                    {
                        task.Cancel();
                    }

                    task.Remove();
                },
                obj =>
                {
                    var task = obj as TaskBase;
                    return task != null;
                }, "download"));


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
               d => CurrentProcessTasks.RemoveElementsNoReturn(d2=>d2.IsSelected,d2=>d2.Cancel()), null,"delete"));

            BindingCommands.ChildActions.Add(taskListAction);

            BindingCommands.ChildActions.Add(taskListAction);

            var processAction = new BindingAction();


            processAction.ChildActions.Add(new Command("配置", obj =>
            {
                var process = GetProcess(obj);
                if (process == null) return;
                ShowConfigUI(process);
            }, obj => true, "settings"));
            processAction.ChildActions.Add(new Command("查看视图", obj =>
            {
                var process = GetProcess(obj);
                if (process == null) return;
                (MainFrmUI as IDockableManager).ActiveModelContent(process);
            }, obj => true, "tv"));
            processAction.ChildActions.Add(new Command("拷贝", obj =>
            {
                var process = GetProcess(obj);
                if (process == null) return;
                ProcessCollection.Remove(obj as IDataProcess);
                var item = GetOneInstance(process.TypeName, true, false);
                (process as IDictionarySerializable).DictCopyTo(item as IDictionarySerializable);
                item.Init();
                ProcessCollection.Add(item);
            }, obj => true, "new"));

            processAction.ChildActions.Add(new Command("移除", obj =>
            {
                var process = GetProcess(obj);
                if (process == null) return;

                RemoveOperation(process);
                ProcessCollection.Remove(process);
                ShowConfigUI(null);
            }, obj => true, "delete"));

            processAction.ChildActions.Add(new Command("保存任务", obj =>
            {
                var process = obj as IDataProcess;
                if (process == null)
                    return;
                SaveTask(process, true);
            }, obj => obj is IDictionarySerializable));
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
            }));
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
                var taskView = PluginProvider.GetObjectInstance<ICustomView>("任务管理视图");
                var userControl = taskView as UserControl;
                if (userControl != null)
                {
                    userControl.DataContext = this;
                    ((INotifyCollectionChanged) CurrentProcessTasks).CollectionChanged += (s, e) =>
                    {
                        ControlExtended.UIInvoke(() => {
                            if (e.Action == NotifyCollectionChangedAction.Add)
                            {
                                dockableManager.ActiveThisContent("任务管理视图");
                            }
                        });
                     
                    }
                        ;
                    dockableManager.AddDockAbleContent(taskView.FrmState, this, taskView, "任务管理视图");
                }
                ProcessCollectionView = new ListCollectionView(ProcessCollection);
                ProcessCollectionView.GroupDescriptions.Clear();
                ProcessCollectionView.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));


                ProjectTaskList = new ListCollectionView(CurrentProject.Tasks);
                ProjectTaskList.GroupDescriptions.Clear();

                ProjectTaskList.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));


                OnPropertyChanged("ProjectTaskList");
                ProjectTaskList = new ListCollectionView(CurrentProject.Tasks);
                ProjectTaskList.GroupDescriptions.Clear();

                ProjectTaskList.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
                OnPropertyChanged("ProjectTaskList");
            }

            var file = MainFrmUI.CommandCollection.FirstOrDefault(d => d.Text == "文件");
            file.ChildActions.Add(new BindingAction("新建项目", obj => CreateNewProject()));
            file.ChildActions.Add(new BindingAction("加载项目", obj => LoadProject()));
            file.ChildActions.Add(new BindingAction("保存项目", obj => SaveCurrentProject()));
            file.ChildActions.Add(new BindingAction("项目另存为", obj => SaveCurrentProject(false)));

            return true;
        }

        private void SaveTask(IDataProcess process, bool haveui)
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
            }
        }

        public ListCollectionView CurrentProcessView { get; set; }
        public ListCollectionView ProcessCollectionView { get; set; }
        public ListCollectionView ProjectTaskList { get; set; }

        private void LoadProject()
        {
            var project = Project.Load();
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

        #endregion

        #region Implemented Interfaces

        #region IProcessManager

        private Project currentProject;
        private TaskBase _selectedTask;


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
                        var count = this.CurrentProcessCollections.Count(d => d.Name.Contains( process.TypeName));
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
        }

        private void NotifyCurrentProjectChanged()
        {
            if (OnCurrentProjectChanged != null)
                OnCurrentProjectChanged(this, new EventArgs());
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
            var view = PluginManager.AddCusomView(MainFrmUI as IDockableManager, rc.TypeName, rc as IView);
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
}