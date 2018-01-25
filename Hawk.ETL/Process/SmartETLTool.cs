using System;
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
using Hawk.ETL.Plugins.Transformers;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;

namespace Hawk.ETL.Process
{
    [XFrmWork("数据清洗", "可方便的对表格数据整理，分组，筛选和排序"
        , groupName: "数据采集和处理")]
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
            AllETLTools.AddRange(
                PluginProvider.GetPluginCollection(typeof (IColumnProcess)));
            if (MainDescription.IsUIForm)
            {
                ETLToolsView = new ListCollectionView(AllETLTools);
                ETLToolsView.GroupDescriptions.Clear();

                ETLToolsView.GroupDescriptions.Add(new PropertyGroupDescription("Self", new GroupConverter()));
            }
        }

        #endregion

        #region Constants and Fields

        private ListBox alltoolList;

        private DataGrid dataView;

        private ListView currentToolList;
        private ScrollViewer scrollViewer;
        private string searchText;

        #endregion

        #region Properties

        [LocalizedDisplayName("命令")]
        [PropertyOrder(3)]
        [LocalizedCategory("4.执行")]
        public ReadOnlyCollection<ICommand> Commands3
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("执行", obj => ExecuteAllExecutors())
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
                        new Command("配置属性", obj => DropAction("Click", obj), obj => obj != null),
                        new Command("删除节点", obj => DropAction("Delete", obj), obj => obj != null),
                        new Command("清空所有工具", obj =>
                        {
                            var item = obj as SmartGroup;
                            foreach (var ColumnProcess in item.Value)
                            {
                                CurrentETLTools.Remove(ColumnProcess);
                            }
                            RefreshSamples();
                        }, obj => obj != null)
                    });
            }
        }

        private void InsertModule(IColumnProcess tool)
        {
            if (ETLMount < 1 || ETLMount >= CurrentETLTools.Count)
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
                    RefreshSamples();
                }
            }
        }


        [Browsable(false)]
        public int AllETLMount => CurrentETLTools.Count;


        [Browsable(false)]
        [LocalizedCategory("3.调试")]
        [PropertyOrder(1)]
        [LocalizedDisplayName("采样量")]
        [LocalizedDescription("只获取数据表的前n行")]
        public int SampleMount
        {
            get { return _SampleMount; }
            set
            {
                if (_SampleMount == value) return;
                _SampleMount = value;
                RefreshSamples();
                OnPropertyChanged("SampleMount");
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
                        new Command("刷新", obj => { RefreshSamples(true); }, icon: "refresh"),
                        new Command("弹出样例", obj => { RefreshSamples(); }, icon: "calendar"),
                        new Command("上一步", obj =>
                        {
                            if (ETLMount > 0)
                                ETLMount--;
                        }, obj => ETLMount > 0, "arrow_left"),
                        new Command("下一步", obj =>
                        {
                            ETLMount++;
                            if (CurrentTool != null)
                            {
                                XLogSys.Print.Info("插入工作模块，名称:" + CurrentTool?.ToString());
                            }
                        }, obj =>  ETLMount < CurrentETLTools.Count , icon: "arrow_right"),
                        new Command("回退到开头", obj => { ETLMount = 0; }, icon: "align_left"),
                        new Command("跳到最后", obj => { ETLMount = CurrentETLTools.Count; }, icon: "align_right")
                    }
                    );
            }
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
                        (MainFrm as IDockableManager).AddDockAbleContent(FrmState.Float, debugGrid, "调试模块属性");
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
        [LocalizedCategory("2.清洗流程")]
        [LocalizedDisplayName("已加载")]
        [LocalizedDescription("当前位于工作流中的的所有工作模块")]
        public ObservableCollection<IColumnProcess> CurrentETLTools { get; set; }


        [Browsable(false)]
        public FrmState FrmState => FrmState.Large;

        [Browsable(false)]
        public virtual object UserControl => null;

        private void ExecuteAllExecutors()
        {
            var has_execute = CurrentETLTools.FirstOrDefault(d => d is IDataExecutor) != null;
            var info = "确定启动执行器?";
            if (!has_execute)
                info = info + "没有在本任务中发现任何执行器。";
            if (MainDescription.IsUIForm &&
                ControlExtended.UserCheck(info, "警告信息"))

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
                        RefreshSamples();
                    };
                }
            };
            return true;
        }

        #endregion

        #region Methods

        private int _SampleMount;
        private  bool shouldUpdate = true;


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


        private EnumerableFunc FuncAdd(IColumnProcess tool, EnumerableFunc func, bool isexecute)
        {
            try
            {
                tool.SetExecute(isexecute);
                tool.Init(new List<IFreeDocument>());
            }
            catch (Exception ex)
            {
                XLogSys.Print.Error($"位于{tool.Column}列的{tool.TypeName}模块在初始化时出现异常：{ex},请检查任务参数");
                return func;
            }
            if (!tool.Enabled)
                return func;
            if (tool is IColumnDataTransformer)
            {
                var ge = tool as IColumnDataTransformer;
                var func1 = func;
                func = d =>
                {
                    if (ge.IsMultiYield)
                    {
                        return ge.TransformManyData(func1(d));
                    }
                    var r = func1(d);
                    return r.Select(d2 => Transform(ge, d2));
                };
            }

            if (tool is IColumnGenerator)
            {
                var ge = tool as IColumnGenerator;

                var func1 = func;
                switch (ge.MergeType)
                {
                    case MergeType.Append:

                        func = d => func1(d).Concat(ge.Generate());
                        break;
                    case MergeType.Cross:
                        func = d => func1(d).Cross(ge.Generate);
                        break;

                    case MergeType.Merge:
                        func = d => func1(d).MergeAll(ge.Generate());
                        break;
                    case MergeType.Mix:
                        func = d => func1(d).Mix(ge.Generate());
                        break;
                }
            }


            if (tool is IDataExecutor && isexecute)
            {
                var ge = tool as IDataExecutor;
                var func1 = func;
                func = d => ge.Execute(func1(d));
            }
            else if (tool is IColumnDataFilter)
            {
                var t = tool as IColumnDataFilter;

                if (t.TypeName == "数量范围选择")
                {
                    dynamic range = t;
                    var func1 = func;
                    func = d => func1(d).Skip((int) range.Skip).Take((int) range.Take);
                }
                else

                {
                    var func1 = func;
                    func = d => func1(d).Where(t.FilteData);
                }
            }
            return func;
        }


        public EnumerableFunc Aggregate(EnumerableFunc func, IEnumerable<IColumnProcess> tools, bool isexecute)
        {
            return tools.Aggregate(func, (current, tool) => FuncAdd(tool, current, isexecute));
        }


        public void ExecuteDatas()
        {
            var etls = CurrentETLTools.Take(ETLMount).Where(d => d.Enabled).ToList();
            EnumerableFunc func = d => d;
            var index = 0;


            if (GenerateMode == GenerateMode.串行模式)
            {
                var generator = etls.FirstOrDefault() as IColumnGenerator;
                if (generator == null)
                    return;
                var realfunc3 = Aggregate(func, etls.Skip(1), true);
                var task = TemporaryTask.AddTempTask(Name + "串行任务", generator.Generate(),
                    d => { realfunc3(new List<IFreeDocument> {d}).ToList(); }, null, generator.GenerateCount() ?? (-1));
                SysProcessManager.CurrentProcessTasks.Add(task);
            }
            else
            {
                var timer = new DispatcherTimer();
                TemporaryTask paratask = null;
                var tolistTransformer = etls.FirstOrDefault(d => d.TypeName == "启动并行") as ToListTF;

                if (tolistTransformer != null)
                {
                    index = etls.IndexOf(tolistTransformer);

                    var beforefunc = Aggregate(func, etls.Take(index), true);
                    var taskbuff = new List<IFreeDocument>();
                    paratask = TemporaryTask.AddTempTask("清洗任务并行化", beforefunc(new List<IFreeDocument>())
                        ,
                        d2 =>
                        {
//TODO:这种分组方式可能会丢数据！！
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
                                name = "清洗任务";

                            var rcount = -1;
                            int.TryParse(countstr, out rcount);
                            var afterfunc = Aggregate(func, etls.Skip(index + 1), true);
                            var task = TemporaryTask.AddTempTask(name, afterfunc(newtaskbuff), d => { },
                                null, rcount, false);
                            if (tolistTransformer.DisplayProgress)
                                ControlExtended.UIInvoke(() => SysProcessManager.CurrentProcessTasks.Add(task));
                            task.Start();
                        }, d => timer.Stop(), -1, false);
                }
                else
                {
                    var generator = etls.FirstOrDefault() as IColumnGenerator;
                    if (generator == null)
                        return;
                    var realfunc3 = Aggregate(func, etls.Skip(1), true);
                    paratask = TemporaryTask.AddTempTask("并行清洗任务", generator.Generate(),
                        d =>
                        {
                            if (paratask.IsPause == false &&
                                SysProcessManager.CurrentProcessTasks.Count > MaxThreadCount)
                            {
                                iswait = true;
                                paratask.IsPause = true;
                            }
                            var task = TemporaryTask.AddTempTask("子任务", realfunc3(new List<IFreeDocument> {d}),
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
            var keys = new[] { "Type", "Group", "Column", "NewColumn" };
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
                    this.shouldUpdate = false;
                    InsertModule(item);
                    this.shouldUpdate = true;
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
                    if (oldProp.IsEqual(attr.UnsafeDictSerializePlus()) == false)
                        RefreshSamples();
                };
                window.ShowDialog();
            }
            if (sender != "Delete") return true;
            var a = attr as IColumnProcess;
            if (MessageBox.Show("确实要删除" + a.TypeName + "吗?", "提示信息", MessageBoxButton.OKCancel) !=
                MessageBoxResult.OK) return true;
            CurrentETLTools.Remove(a);
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


        private IFreeDocument Transform(IColumnDataTransformer ge,
            IFreeDocument item)
        {
            if (item == null)
                return new FreeDocument();

            var dict = item;

            object res = null;
            try
            {
                if (ge.OneOutput && dict[ge.Column] == null)
                {
                }
                else
                {
                    res = ge.TransformData(dict);
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
                XLogSys.Print.Error(ex.ToString());
            }

            if (ge.OneOutput)
            {
                if (!string.IsNullOrWhiteSpace(ge.NewColumn))
                {
                    if (res != null)
                    {
                        dict.SetValue(ge.NewColumn, res);
                    }
                }
                else
                {
                    dict.SetValue(ge.Column, res);
                }
            }


            return dict;
        }


        [PropertyOrder(1)]
        [LocalizedCategory("4.执行")]
        [LocalizedDisplayName("工作模式")]
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
        [LocalizedCategory("4.执行")]
        [LocalizedDescription("在并行模式工作时，线程池所承载的最大线程数")]
        [LocalizedDisplayName("最大线程数")]
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
                        XLogSys.Print.Warn("最大线程数的数值范围为0-30");
                    }
                    if (value <= 0)
                        value = 1;
                    _maxThreadCount = value;
                    OnPropertyChanged("MaxThreadCount");
                }
            }
        }


        private int _etlMount = 0;

        public IEnumerable<IFreeDocument> Generate(IEnumerable<IColumnProcess> processes, bool isexecute,
            IEnumerable<IFreeDocument> source = null)

        {
            if (source == null)
                source = new List<IFreeDocument>();
            var func = Aggregate(d => d, processes, isexecute);
            return func(source);
        }

        private bool mudoleHasInit;
        private int _maxThreadCount;
        private GenerateMode _generateMode;
        private ListViewDragDropManager<IColumnProcess> dragMgr;
        private bool isErrorRemind = true;
        List<string> all_columns = new List<string>();
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
                var str = $"{Name}已经有任务在执行，由于调整参数，是否要取消当前任务重新执行？\n【取消】:【不再提醒】";
                if (isErrorRemind == false)
                {
                    XLogSys.Print.Warn($"{Name}已经有任务在执行，请在任务管理器中取消该任务后再刷新");
                    return;
                }
                if (!MainDescription.IsUIForm)
                    return;
                    var result =
                        MessageBox.Show(str, "提示信息", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (var item in tasks)
                        {
                            item.Remove();
                        }
                        XLogSys.Print.Warn(str + "  已经取消");
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
                            if (oldProp.IsEqual(process.UnsafeDictSerializePlus()) == false)
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


            var alltools = CurrentETLTools.Take(ETLMount).ToList();
            var func = Aggregate(d => d, alltools, false);
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
            var temptask = TemporaryTask.AddTempTask(Name + "_转换",
                func(new List<IFreeDocument>()).Take(SampleMount),
                data =>
                {
                    ControlExtended.UIInvoke(() =>
                    {
                        foreach (var key in data.GetKeys().Where(d => all_columns.Contains(d) == false))
                        {
                            AddColumn(key, alltools);
                            all_columns.Add(key);
                        }

                        Documents.Add((data));
                        InitUI();
                    });
                }, r =>
                {
                    var tool = CurrentTool;


                    if (tool != null)
                    {
                        SmartGroupCollection.Where(d => d.Name == tool.Column)
                            .Execute(d => d.GroupType = GroupType.Input);
                        var transformer = tool as IColumnDataTransformer;
                        if (transformer != null)
                        {
                            var newcol = transformer.NewColumn.Split(' ');
                            if (transformer.IsMultiYield)
                            {
                                SmartGroupCollection.Execute(
                                    d => d.GroupType = newcol.Contains(d.Name) ? GroupType.Input : GroupType.Output);
                            }
                            else
                            {
                                SmartGroupCollection.Where(d => d.Name == transformer.NewColumn)
                                    .Execute(d => d.GroupType = GroupType.Output);
                                ;
                            }
                        }
                    }
                    var nullgroup = SmartGroupCollection.FirstOrDefault(d => string.IsNullOrEmpty(d.Name));
                    nullgroup?.Value.AddRange(
                        alltools.Where(
                            d =>
                                Documents.GetKeys().Contains(d.Column) == false &&
                                string.IsNullOrEmpty(d.Column) == false));
                    nullgroup.OnPropertyChanged("Value");
                }
                , SampleMount);
            temptask.Publisher = this;
            SysProcessManager.CurrentProcessTasks.Add(temptask);
        }

        private void AddColumn(string key, IEnumerable<IColumnProcess> alltools)
        {
            var col = new DataGridTemplateColumn
            {
                Header = key,
                Width = 155
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
                    if (last != null && last.TypeName == "列名修改器" && last.NewColumn == key)
                    {
                        last.NewColumn = group.Name;
                    }
                    else
                    {
                        last = PluginProvider.GetObjectInstance("列名修改器") as IColumnDataTransformer;
                        last.NewColumn = group.Name;
                        last.Column = key;
                        InsertModule(last);
                        ETLMount++;
                        OnPropertyChanged("ETLMount");
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

    public class SmartGroup : PropertyChangeNotifier
    {
        private string _name;

        #region Properties

        public ColumnInfo ColumnInfo { get; set; }
        public int Index { get; set; }

        public GroupType GroupType { get; set; }

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