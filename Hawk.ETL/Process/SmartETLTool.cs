using System;
using Hawk.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Plugins.Transformers;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;

namespace Hawk.ETL.Process
{
    [XFrmWork("key_201", "SmartETLTool_desc" ,url: "diagram",  groupName: "数据采集和处理")]
    public class SmartETLTool : AbstractProcessMethod, IView
    {
        #region Constructors and Destructors

        public SmartETLTool()
        {
            AllETLTools = new List<XFrmWorkAttribute>();
            CurrentETLTools = new ObservableCollection<IColumnProcess>();
            SmartGroupCollection = new ObservableCollection<SmartGroup>();
            Documents = new ObservableCollection<IFreeDocument>();
            SampleMount = 20;
            MaxThreadCount = 20;
            IsUISupport = true;
            Analyzer = new Analyzer();
       
            IsAutoRefresh = true;
            AllETLTools.AddRange(
                PluginProvider.GetPluginCollection(typeof (IColumnProcess)));
            if (MainDescription.IsUIForm)
            {
                ETLToolsView = new ListCollectionView(AllETLTools);
                ETLToolsView.GroupDescriptions.Clear();

                ETLToolsView.GroupDescriptions.Add(new PropertyGroupDescription("Self", new GroupConverter()));
                ETLToolsView.SortDescriptions.Add(new SortDescription("GroupName",ListSortDirection.Ascending));
                ETLToolsView.CustomSort =  new NameComparer();
                
            }
        }

        #endregion

        #region Constants and Fields

        private ListBox alltoolList;

        public Analyzer Analyzer { get; set; }
        private DataGrid dataView;

        private ListView currentToolList;
        private ScrollViewer scrollViewer;
        private string searchText = GlobalHelper.Get("key_110");

        #endregion

        #region Properties

        [LocalizedDisplayName("key_677")]
        [PropertyOrder(3)]
        [LocalizedCategory("key_678")]
        public ReadOnlyCollection<ICommand> Commands3
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_34"), obj => ExecuteAllExecutors(), icon: "play")
                    });
            }
        }


        [Browsable(false)]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_631"), obj => DropAction("Click", obj), obj => obj != null, "settings"),
                        new Command(GlobalHelper.Get("key_679"), obj => DropAction("Delete", obj), obj => obj != null, "delete"),
                        new Command(GlobalHelper.Get("key_680"), obj =>
                        {
                            var item = obj as SmartGroup;
                            foreach (var ColumnProcess in item.Value)
                            {
                                CurrentETLTools.Remove(ColumnProcess);
                            }
                            RefreshSamples();
                        }, obj => obj != null, "clear"),
                        new Command(GlobalHelper.Get("key_681"), obj =>
                        {
                            var item = obj as IColumnProcess;
                            var newitem = PluginProvider.GetObjectInstance<IColumnProcess>(item.TypeName);
                            item.DictCopyTo(newitem);
                            CurrentETLTools.Insert(CurrentETLTools.IndexOf(item), newitem);
                        }, obj => obj != null, "clipboard_file"),
                        new Command(GlobalHelper.Get("key_682"), obj =>
                        {
                            var item = obj as IColumnProcess;
                            var index = CurrentETLTools.IndexOf(item);
                            CurrentETLTools.Move(index, index - 1);
                        }, obj => obj != null, "arrow_up"),
                        new Command(GlobalHelper.Get("key_683"), obj =>
                        {
                            var item = obj as IColumnProcess;
                            var index = CurrentETLTools.IndexOf(item);
                            CurrentETLTools.Move(index, index + 1);
                        }, obj => obj != null, "arrow_down"),
                        new Command(GlobalHelper.Get("key_684"), obj => { ETLMount = CurrentETLTools.IndexOf(obj as IColumnProcess); },
                            obj => obj != null, "tag"),
                              new Command(GlobalHelper.Get("key_685"), obj =>
                              {
                                  var index= CurrentETLTools.IndexOf(obj as IColumnProcess);
                                  CurrentETLTools.KeepRange(0,index+1);
                                  ETLMount = index + 1;
                              },
                            obj => obj != null, "tag")
                    });
            }
        }

        private void InsertModule(IColumnProcess tool)
        {
            if (ETLMount < 0 || ETLMount >= CurrentETLTools.Count)
                CurrentETLTools.Add(tool);
            else
            {
                CurrentETLTools.Insert(ETLMount, tool);
            }
        }

        [Browsable(false)]
        public ObservableCollection<SmartGroup> SmartGroupCollection { get; set; }

        [Browsable(false)]
        public ObservableCollection<IFreeDocument> Documents { get; set; }

        [Browsable(false)]
        public ListCollectionView ETLToolsView { get; set; }

        internal bool isRemainCloseTask;

        [Browsable(false)]
        public int ETLMount
        {
            get { return _etlMount; }
            set
            {
                if (_etlMount != value)
                {
                    _etlMount = value;
                    OnPropertyChanged("ETLMount");
                    OnPropertyChanged("CurrentTool");
                    if (IsAutoRefresh == false)
                        return;
                    RefreshSamples();
                }
            }
        }


        [Browsable(false)]
        public int AllETLMount => CurrentETLTools.Count;


        [Browsable(false)]
        [LocalizedCategory("key_686")]
        [PropertyOrder(1)]
        [LocalizedDisplayName("key_687")]
        [LocalizedDescription("key_688")]
        public int SampleMount
        {
            get { return _SampleMount; }
            set
            {
                if (_SampleMount == value) return;
                _SampleMount = value;
                OnPropertyChanged("SampleMount");
                if (IsAutoRefresh == false)
                    return;
                RefreshSamples();
            }
        }

        [Browsable(false)]
        public bool DisplayDetail { get; set; }

        [Browsable(false)]
        public ReadOnlyCollection<ICommand> Commands5
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_142"), obj => { RefreshSamples(true); }, icon: "refresh"),
                        new Command(GlobalHelper.Get("key_689"), obj => { RefreshSamples(); }, icon: "calendar"),
                        new Command(GlobalHelper.Get("key_690"), obj =>
                        {
                            if (ETLMount > 0)
                                ETLMount--;
                        }, obj => ETLMount > 0, "arrow_left"),
                        new Command(GlobalHelper.Get("key_691"), obj =>
                        {
                            ETLMount++;
                            if (CurrentTool != null)
                            {
                                XLogSys.Print.Info(GlobalHelper.Get("key_692") + CurrentTool?.ToString());
                            }
                        }, obj => ETLMount < CurrentETLTools.Count, "arrow_right"),
                        new Command(GlobalHelper.Get("key_693"), obj => { ETLMount = 0; }, icon: "align_left"),
                        new Command(GlobalHelper.Get("key_694"), obj => { ETLMount = CurrentETLTools.Count; }, icon: "align_right"),
                        new Command(GlobalHelper.Get("key_695"), obj => { EnterAnalyzer(); }, icon: "magnify_add")
                    }
                    );
            }
        }

        private void EnterAnalyzer()
        {
            var view = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("key_696")) as UserControl;
            view.DataContext = Analyzer;
             
            ControlExtended.DockableManager.AddDockAbleContent(
                FrmState.Custom, view, GlobalHelper.Get("key_697"));
        }

        private WPFPropertyGrid debugGrid;

        [Browsable(false)]
        public IColumnProcess CurrentTool
        {
            get
            {
                var t = CurrentETLTools.Where(d => !(d is IDataExecutor) && d.Enabled).ToList();
                IColumnProcess current = null;
                if (ETLMount <= t.Count && ETLMount > 1)
                {
                    current = t[ETLMount - 1];
                }
                if (DisplayDetail)
                {
                    if (debugGrid == null)
                    {
                        debugGrid = PropertyGridFactory.GetInstance(current);
                    }
                    else
                    {
                        debugGrid.SetObjectView(current);
                    }
                    dynamic control =
                        (MainFrm as IDockableManager).ViewDictionary.FirstOrDefault(d => d.View == debugGrid)
                            ?.Container;
                    if (control != null)
                    {
                        control.Show();
                    }

                    else
                    {
                        (MainFrm as IDockableManager).AddDockAbleContent(FrmState.Float, debugGrid, GlobalHelper.Get("key_698"));
                    }
                }
                else
                {
                    dynamic control =
                        (MainFrm as IDockableManager).ViewDictionary.FirstOrDefault(d => d.View == debugGrid)
                            ?.Container;
                    if (control != null)
                        control.Close();
                    debugGrid = null;
                }


                return current;
            }
            set
            {
                var t = CurrentETLTools.Where(d => !(d is IDataExecutor) && d.Enabled).ToList();
                ETLMount = CurrentETLTools.IndexOf(value) + 1;
            }
        }

        [Browsable(false)]
        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (searchText == value) return;
                searchText = value;
                if (ETLToolsView.CanFilter)
                {
                    ETLToolsView.Filter = FilterMethod;
                }
                OnPropertyChanged("SearchText");
            }
        }

        [Browsable(false)]
        protected List<XFrmWorkAttribute> AllETLTools { get; set; }

        [Browsable(false)]
        public dynamic etls => CurrentETLTools;

        [Browsable(false)]
        [LocalizedCategory("key_699")]
        [LocalizedDisplayName("key_700")]
        [LocalizedDescription("key_701")]
        public ObservableCollection<IColumnProcess> CurrentETLTools { get; set; }


        [Browsable(false)]
        public FrmState FrmState => FrmState.Large;

        [Browsable(false)]
        public virtual object UserControl => null;

        private void ExecuteAllExecutors()
        {
            var has_execute = CurrentETLTools.FirstOrDefault(d => d is IDataExecutor) != null;
            var info = GlobalHelper.Get("key_702");
            if (!has_execute)
                info = info + GlobalHelper.Get("key_703");
            if (MainDescription.IsUIForm &&
                ControlExtended.UserCheck(info, GlobalHelper.Get("key_151")))

            {
                ExecuteDatas();
            }
        }

        #endregion

        #region Public Methods

        public override void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            shouldUpdate = false;
            base.DictDeserialize(dicts, scenario);
            MaxThreadCount = dicts.Set("MaxThreadCount", MaxThreadCount);
            GenerateMode = dicts.Set("GenerateMode", GenerateMode);
            SampleMount = dicts.Set("SampleMount", SampleMount);
            var doc = dicts as FreeDocument;
            if (doc != null && doc.Children != null)
            {
                foreach (var child in doc.Children)
                {
                    var name = child["Type"].ToString();
                    var process = PluginProvider.GetObjectByType<IColumnProcess>(name);
                    if (process != null)
                    {
                        process.DictDeserialize(child);
                        CurrentETLTools.Add(process);
                        process.Father = this;
                        var tool = process as ToolBase;
                        if (tool != null)
                            tool.ColumnSelector.GetItems = () => all_columns;
                    }
                }
            }
            ETLMount = CurrentETLTools.Count;
            shouldUpdate = true;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize(scenario);
            dict.Add("MaxThreadCount", MaxThreadCount);
            dict.Add("GenerateMode", GenerateMode);
            dict.Add("SampleMount", SampleMount);
            dict.Children = new List<FreeDocument>();
            dict.Children.AddRange(CurrentETLTools.Select(d => d.DictSerialize(scenario)));
            return dict;
        }

        public override bool Init()
        {
            mudoleHasInit = true;
            Analyzer.DataManager = this.SysDataManager;
            RefreshSamples();
            CurrentETLTools.CollectionChanged += (s, e) =>
            {
                if (e.Action != NotifyCollectionChangedAction.Add) return;
                foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
                {
                    var tool = item as ToolBase;
                    if (tool != null)
                        tool.ColumnSelector.GetItems = () => all_columns;
                    item.PropertyChanged += (s2, e2) =>
                    {
                        if (shouldUpdate == false)
                            return;
                        if (e2.PropertyName == "Enabled")
                        {
                            var should = s2 as IColumnProcess;
                            if (should.Enabled == false)
                                return;
                        }
                        if (IsAutoRefresh == false)
                            return;
                        //RefreshSamples();
                    };
                }
            };
            return true;
        }

        #endregion

        #region Methods

        private int _SampleMount;
        private bool shouldUpdate = true;

        [Browsable(false)]
        public bool IsAutoRefresh
        {
            get { return _isAutoRefresh; }
            set
            {
                if (_isAutoRefresh != value)
                {
                    _isAutoRefresh = value;
                    OnPropertyChanged("IsAutoRefresh");
                }
            }
        }

        [Browsable(false)]
        public bool IsUISupport { get; set; }


        public void InitProcess(bool isexecute)
        {
            foreach (var item in CurrentETLTools.Where(d => d.Enabled))
            {
                if (isexecute == false && item is IDataExecutor)
                {
                    continue;
                }
                item.Init(new List<IFreeDocument>());
            }
        }


      
        public void ExecuteDatas()
        {
            var etls = CurrentETLTools.Take(ETLMount).Where(d => d.Enabled).ToList();
            var index = 0;

            Analyzer.Start(this.Name);
            if (GenerateMode == GenerateMode.串行模式)
            {
                
                var realfunc3 = etls.Aggregate(isexecute:  true,analyzer:Analyzer);
                var task = TemporaryTask.AddTempTask(Name + GlobalHelper.Get("key_704"), realfunc3.Invoke(),
                    null);
                task.IsSelected = true;
                SysProcessManager.CurrentProcessTasks.Add(task);
            }
            else
            {
                var timer = new DispatcherTimer();
                TemporaryTask paratask = null;
                var tolistTransformer = etls.FirstOrDefault(d => d.TypeName == GlobalHelper.Get("ToListTF")) as ToListTF;

                if (tolistTransformer != null)
                {
                    index = etls.IndexOf(tolistTransformer);

                    var beforefunc = etls.Take(index).Aggregate(isexecute:  true, analyzer: Analyzer);
                    var taskbuff = new List<IFreeDocument>();
                    paratask = TemporaryTask.AddTempTask(GlobalHelper.Get("key_705"), beforefunc(new List<IFreeDocument>())
                        ,
                        d2 =>
                        {
                            if (taskbuff.Count < tolistTransformer.GroupMount)
                            {
                                taskbuff.Add(d2);
                                return;
                            }
                            var newtaskbuff = taskbuff.ToList();
                            taskbuff.Clear();
                            if (paratask.IsPause == false &&
                                SysProcessManager.CurrentProcessTasks.Count > MaxThreadCount)
                            {
                                iswait = true;
                                paratask.IsPause = true;
                            }
                            var countstr = d2.Query(tolistTransformer.MountColumn);
                            var name = d2.Query(tolistTransformer.IDColumn);
                            if (name == null)
                                name = GlobalHelper.Get("key_706");

                            var rcount = -1;
                            int.TryParse(countstr, out rcount);
                            var afterfunc = etls.Skip(index + 1).Aggregate(isexecute:true);
                            var task = TemporaryTask.AddTempTask(name, afterfunc(newtaskbuff), d => { },
                                null, rcount, false);
                            if (tolistTransformer.DisplayProgress)
                                ControlExtended.UIInvoke(() => SysProcessManager.CurrentProcessTasks.Add(task));
                            task.Start();
                        }, d => timer.Stop(), -1, false);
                }
                else
                {
                    var paraPoint = etls.GetParallelPoint();
                    var beforefunc = etls.Take(paraPoint).Aggregate(isexecute: true);
                    var generator = etls.FirstOrDefault() as IColumnGenerator;
                    if (generator == null)
                        return;
                    var afterfunc = etls.Skip(paraPoint).Aggregate(isexecute: true);
                    paratask = TemporaryTask.AddTempTask(GlobalHelper.Get("key_705"), beforefunc(new List<IFreeDocument>()),
                        d =>
                        {
                            if (paratask.IsPause == false &&
                                SysProcessManager.CurrentProcessTasks.Count > MaxThreadCount)
                            {
                                iswait = true;
                                paratask.IsPause = true;
                            }
                            var task = TemporaryTask.AddTempTask(GlobalHelper.Get("key_707"), afterfunc(new List<IFreeDocument> {d}),
                                d2 => { },
                                null, 1, false);
                            ControlExtended.UIInvoke(() => SysProcessManager.CurrentProcessTasks.Add(task));
                            task.Start();
                        }, d => timer.Stop(), generator.GenerateCount() ?? (-1), false);
                }
                SysProcessManager.CurrentProcessTasks.Add(paratask);

                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) =>
                {
                    if (paratask.IsCanceled)
                    {
                        timer.Stop();
                        return;
                    }


                    if (paratask.IsStart == false)
                    {
                        paratask.Start();
                        return;
                    }

                    if (iswait && SysProcessManager.CurrentProcessTasks.Count < MaxThreadCount)
                    {
                        paratask.IsPause = false;
                        iswait = false;
                    }
                };

                timer.Start();
            }
        }

        private bool iswait;

        private bool NeedConfig(IDictionarySerializable item)
        {
            var config = item.DictSerialize();
            var keys = new[] {"Type", "Group", "Column", "NewColumn"};
            foreach (var k in config.DataItems)
            {
                if (keys.Contains(k.Key))
                    continue;
                if (k.Value == null || k.Value.ToString() == "")
                    return true;
            }
            return false;
        }

        private bool DropAction(string sender, object attr)
        {
            if (sender == "Drop")
            {
                var objs = attr as object[];
                if (objs.Count() == 2)
                {
                    var p = objs[0] as SmartGroup;
                    var t = objs[1] as XFrmWorkAttribute;

                    var item = PluginProvider.GetObjectInstance(t.MyType) as IColumnProcess;

                    if (string.IsNullOrEmpty(p.Name) == false)
                        item.Column = p.Name;
                    item.Father = this;
                    shouldUpdate = false;
                    InsertModule(item);
                    shouldUpdate = true;
                    item.ObjectID = String.Format("{0}_{1}_{2}", item.TypeName, item.Column, this.CurrentETLTools.Count);
                    if (NeedConfig(item))
                    {
                        var window = PropertyGridFactory.GetPropertyWindow(item);

                        window.ShowDialog();
                    }
                    ETLMount++;
                }
            }
            if (sender == "Click")
            {
                var smart = attr as SmartGroup;
                if (smart != null)
                {
                    attr = smart.ColumnInfo;
                }
                var window = PropertyGridFactory.GetPropertyWindow(attr);
                var oldProp = attr.UnsafeDictSerializePlus();

                window.Closed += (s, e) =>
                {
                    if (oldProp.IsEqual(attr.UnsafeDictSerializePlus()) == false && IsAutoRefresh)
                        RefreshSamples();
                };
                window.ShowDialog();
            }
            if (sender != "Delete") return true;
            var a = attr as IColumnProcess;
            if (MessageBox.Show(String.Format(GlobalHelper.Get("key_708"),a.TypeName), GlobalHelper.Get("key_99"), MessageBoxButton.OKCancel) !=
                MessageBoxResult.OK) return true;

            CurrentETLTools.Remove(a);
            if (IsAutoRefresh)
                RefreshSamples();
            return true;
        }


        private bool FilterMethod(object obj)
        {
            var process = obj as XFrmWorkAttribute;
            if (process == null)
            {
                return false;
            }
            var text = SearchText.ToLower();
            if (string.IsNullOrWhiteSpace(text))
                return true;
            var texts = new List<string> {process.Name, process.Description};

            foreach (var text1 in texts.ToList())
            {
                var result = text1.Select(FileEx.GetCharSpellCode).Select(spell => spell.ToString()).ToList();
                texts.Add("".Join(result));
            }
            texts.AddRange(texts.Select(d => d.ToLower()).ToList());
            texts.AddRange(texts.Select(d => d.ToUpper()).ToList());
            texts = texts.Distinct().ToList();
            return texts.FirstOrDefault(d => d.Contains(text)) != null;
        }


    

        [PropertyOrder(1)]
        [LocalizedCategory("key_678")]
        [LocalizedDisplayName("key_188")]
        public GenerateMode GenerateMode
        {
            get { return _generateMode; }
            set
            {
                if (_generateMode == value)
                    return;
                _generateMode = value;
                OnPropertyChanged("GenerateMode");
            }
        }


        [PropertyOrder(2)]
        [LocalizedCategory("key_678")]
        [LocalizedDescription("key_709")]
        [LocalizedDisplayName("key_710")]
        [NumberRange(1, 20, 1)]
        public int MaxThreadCount
        {
            get { return _maxThreadCount; }
            set
            {
                if (_maxThreadCount != value)
                {
                    if (value > 30)
                    {
                        value = 30;
                        XLogSys.Print.Warn(GlobalHelper.Get("key_711"));
                    }
                    if (value <= 0)
                        value = 1;
                    _maxThreadCount = value;
                    OnPropertyChanged("MaxThreadCount");
                }
            }
        }


        private int _etlMount;

 

        private bool mudoleHasInit;
        private int _maxThreadCount;
        private GenerateMode _generateMode;
        private ListViewDragDropManager<IColumnProcess> dragMgr;
        private bool isErrorRemind = true;
        private readonly List<string> all_columns = new List<string>();
        private bool _isAutoRefresh;
        private DateTime lastRefreshTime;

        public void RefreshSamples(bool canGetDatas = true)
        {
       
            if (shouldUpdate == false)
                return;

            if (SysProcessManager == null)
                return;
            if (!mudoleHasInit)
                return;
            OnPropertyChanged("AllETLMount");

            var tasks = SysProcessManager.CurrentProcessTasks.Where(d => d.Publisher == this).ToList();
            if (tasks.Any())
            {
                var str = String.Format(GlobalHelper.Get("task_run"),Name);
                if (isErrorRemind == false)
                {
                    XLogSys.Print.Warn(string.Format(GlobalHelper.Get("key_712"),Name));
                    return;
                }
                if (!MainDescription.IsUIForm)
                    return;
                var result =
                    MessageBox.Show(str, GlobalHelper.Get("key_99"), MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    foreach (var item in tasks)
                    {
                        item.Remove();
                    }
                    XLogSys.Print.Warn(str + GlobalHelper.Get("key_713"));
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    isErrorRemind = false;
                }
                else
                {
                    return;
                }
            }
            if (dataView == null && MainDescription.IsUIForm && IsUISupport)
            {
                var dock = MainFrm as IDockableManager ?? ControlExtended.DockableManager;
                var control = dock?.ViewDictionary.FirstOrDefault(d => d.Model == this);
                if (control != null)
                {
                    if (control.View is IRemoteInvoke)
                    {
                        var invoke = control.View as IRemoteInvoke;
                        invoke.RemoteFunc = DropAction;
                    }
                    dynamic dy = control.View;

                    dataView = dy.DataList;
                    scrollViewer = dy.ScrollViewer;

                    alltoolList = dy.ETLToolList;
                    currentToolList = dy.CurrentETLToolList;
                    currentToolList.MouseDoubleClick += (s, e) =>
                    {
                        if (e.ChangedButton != MouseButton.Left)
                        {
                            return;
                        }
                        var process = currentToolList.SelectedItem as IColumnProcess;
                        if (process == null)
                        {
                            return;
                        }
                        var oldProp = process.UnsafeDictSerializePlus();
                        var window = PropertyGridFactory.GetPropertyWindow(process);
                        window.Closed += (s2, e2) =>
                        {
                            if (
                                (oldProp.IsEqual(process.UnsafeDictSerializePlus()) == false && IsAutoRefresh).SafeCheck
                                    (GlobalHelper.Get("key_714"),LogType.Debug))
                                RefreshSamples();
                        };
                        window.ShowDialog();
                    };
                    dragMgr = new ListViewDragDropManager<IColumnProcess>(currentToolList);
                    dragMgr.ShowDragAdorner = true;

                    alltoolList.MouseMove += (s, e) =>
                    {
                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            var attr = alltoolList.SelectedItem as XFrmWorkAttribute;
                            if (attr == null)
                            {
                                return;
                            }

                            var data = new DataObject(typeof (XFrmWorkAttribute), attr);
                            try
                            {
                                DragDrop.DoDragDrop(control.View as UserControl, data, DragDropEffects.Move);
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    };
                }
            }
            if (dataView == null)
                return;
            Analyzer.Items.Clear();

            var alltools = CurrentETLTools.Take(ETLMount).ToList();
            var func = alltools.Aggregate(isexecute: false,analyzer: Analyzer);
            if (!canGetDatas)
                return;
            SmartGroupCollection.Clear();
            Documents.Clear();
            shouldUpdate = false;
            var i = 0;
            foreach (var currentEtlTool in CurrentETLTools)
            {
                (currentEtlTool).ETLIndex = i++;
            }
            shouldUpdate = true;
            if (!MainDescription.IsUIForm)
                return;
            all_columns.Clear();
            dataView.Columns.Clear();

            AddColumn("", alltools);
            var temptask = TemporaryTask.AddTempTask(Name + "_"+ GlobalHelper.Get("key_108"),
                func(new List<IFreeDocument>()).Take(SampleMount),
                data =>
                {
                    ControlExtended.UIInvoke(() =>
                    {
                        foreach (var key in data.GetKeys().Where(d => all_columns.Contains(d) == false).OrderBy(d => d))
                        {
                            AddColumn(key, alltools);
                            DeleteColumn("");
                            all_columns.Add(key);
                        }

                        Documents.Add((data));
                        InitUI();
                    });
                }, r =>
                {
                    var tool = CurrentTool;
                    var outputCol = new List<string>();
                    var inputCol = new List<string>();

                    if (tool != null)
                    {
                        inputCol.Add(tool.Column);

                        var transformer = tool as IColumnDataTransformer;
                        if (transformer != null)
                        {
                            if (transformer is CrawlerTF)
                            {
                                var crawler = transformer as CrawlerTF;
                                outputCol = crawler?.Crawler?.CrawlItems.Select(d => d.Name).ToList();
                            }
                            else if (transformer is ETLBase)
                            {
                                var etl = transformer as ETLBase;
                                var target = etl.GetModule<SmartETLTool>(etl.ETLSelector.SelectItem);
                                outputCol = target?.Documents.GetKeys().ToList();
                                inputCol.AddRange(etl.MappingSet.Split(' ').Select(d => d.Split(':')[0]));
                            }
                            else
                            {
                                outputCol = transformer.NewColumn.Split(' ').ToList();
                            }
                            SmartGroupCollection.Where(d => outputCol != null && outputCol.Contains(d.Name))
                                .Execute(d => d.GroupType = GroupType.Output);
                            SmartGroupCollection.Where(d => inputCol.Contains(d.Name))
                                .Execute(d => d.GroupType = GroupType.Input);
                        }
                    }

                    var firstOutCol = outputCol?.FirstOrDefault();
                    if (firstOutCol != null)
                    {
                        var index = all_columns.IndexOf(firstOutCol);
                        if (index != -1 && ETLMount < AllETLMount)
                        {
                            scrollViewer.ScrollToHorizontalOffset(index*CellWidth);
                        }
                    }
                    var nullgroup = SmartGroupCollection.FirstOrDefault(d => string.IsNullOrEmpty(d.Name));
                    nullgroup?.Value.AddRange(
                        alltools.Where(
                            d =>
                                Documents.GetKeys().Contains(d.Column) == false &&
                                string.IsNullOrEmpty(d.Column) == false));
                    nullgroup?.OnPropertyChanged("Value");
                }
                , SampleMount);
            temptask.Publisher = this;
            temptask.IsSelected = true;
            SysProcessManager.CurrentProcessTasks.Add(temptask);
        }

        public static int CellWidth = 155;

        private void DeleteColumn(string key)
        {
            SmartGroupCollection.RemoveElementsNoReturn(d=>d.Name==key);
            dataView.Columns.RemoveElementsNoReturn(d=>d.Header.ToString()==key);
        }
        private void AddColumn(string key, IEnumerable<IColumnProcess> alltools)
        {
            if (dataView == null)
                return;
            var col = new DataGridTemplateColumn
            {
                Header = key,
                Width = CellWidth
            };
            var dt = new DataTemplate();
            col.CellTemplate = dt;
            var fef = new FrameworkElementFactory(typeof (MultiLineTextEditor));
            var binding = new Binding();

            binding.Path = new PropertyPath(($"[{key}]"));
            fef.SetBinding(ContentControl.ContentProperty, binding);
            fef.SetBinding(MultiLineTextEditor.TextProperty, binding);
            dt.VisualTree = fef;
            col.CellTemplate = dt;
            dataView.Columns.Add(col);

            var group = new SmartGroup
            {
                Name = key,
                Value = new ObservableCollection<IColumnProcess>(alltools.Where(d => d.Column == key))
            };
            group.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Name")
                {
                    var last = alltools.LastOrDefault() as IColumnDataTransformer;
                    if (last != null && last.TypeName == GlobalHelper.Get("RenameTF") && last.NewColumn == key)
                    {
                        last.NewColumn = group.Name;
                    }
                    else
                    {
                        last = PluginProvider.GetObjectInstance(GlobalHelper.Get("RenameTF")) as IColumnDataTransformer;
                        last.NewColumn = group.Name;
                        last.Column = key;
                        InsertModule(last);
                        ETLMount++;
                        OnPropertyChanged("ETLMount");
                        if (IsAutoRefresh)
                            RefreshSamples();
                    }
                }
            };
            SmartGroupCollection.Add(group
                );
        }

        private void InitUI()

        {
        }

        #endregion
    }

    public enum GroupType
    {
        Common,
        Input,
        Output,
        Edit
    }

    public class NameComparer : IComparer
    {
     
        public int Compare(object x, object y)
        {
            var x1 = x as XFrmWorkAttribute;
            var y1 = y as XFrmWorkAttribute;
            var key = GlobalHelper.Get("key_110");
            if (x1.Description.Contains(key))
            {
                if (y1.Description.Contains(key))
                    return x1.Name.CompareTo(y1.Name);
                return -1;

            }
            if (y1.Description.Contains(key))
            {
                return 1;

            }
           return x1.Name.CompareTo(y1.Name);


        }
    }
    public class SmartGroup : PropertyChangeNotifier
    {
        private GroupType _groupType;
        private string _name;

        #region Properties

        public ColumnInfo ColumnInfo { get; set; }
        public int Index { get; set; }

        public GroupType GroupType
        {
            get { return _groupType; }
            set
            {
                if (_groupType != value)
                {
                    _groupType = value;
                    OnPropertyChanged("GroupType");
                }
            }
        }

        /// <summary>
        ///     名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public ObservableCollection<IColumnProcess> Value { get; set; }

        #endregion
    }
}