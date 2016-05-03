using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace Hawk.ETL.Process
{
    [XFrmWork("数据清洗ETL", "可方便的对表格数据整理，分组，筛选和排序",
        "/XFrmWork.DataMining.Process;component/Images/hadoop.jpg", "数据采集和处理")]
    public class SmartETLTool : AbstractProcessMethod, IView
    {
        #region Constructors and Destructors

        public SmartETLTool()
        {
            AllETLTools = new List<XFrmWorkAttribute>();
            CurrentETLTools = new ObservableCollection<IColumnProcess>();
            Dict = new ObservableCollection<SmartGroup>();
            Documents = new ObservableCollection<IFreeDocument>();
            SampleMount = 20;
            MaxThreadCount = 20;
            IsUISupport = true;
            AllETLTools.AddRange(
              PluginProvider.GetPluginCollection(typeof(IColumnProcess)));
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

        private ListView dataView;

        private string searchText;

        #endregion

        #region Properties
     


          [DisplayName("命令")]
        [PropertyOrder(3)]
        [Category("3.执行")]
        public ReadOnlyCollection<ICommand> Commands3
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                         new Command("执行", obj => ExecuteAllExecutors()),

                     
                    });
            }
        }




        [DisplayName("命令")]
        [PropertyOrder(5)]
        [Category("2.清洗流程")]
        public ReadOnlyCollection<ICommand> Commands2
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("刷新结果", obj => { RefreshSamples(); }),
                    
                        new Command("弹出样例", obj =>
                        {
                            generateFloatGrid = true;
                            RefreshSamples();
                        }),
                      
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
                CurrentETLTools.Insert(ETLMount-1,tool);
            }
        }
        [Browsable(false)]
        public ObservableCollection<SmartGroup> Dict { get; set; }

        [Browsable(false)]
        public ObservableCollection<IFreeDocument> Documents { get; set; }

        [Browsable(false)]
        public ListCollectionView ETLToolsView { get; set; }

        [Category("4.调试")]
        [PropertyOrder(2)]
        [DisplayName("模块数量")]
        public int ETLMount
        {
            get
            {
                return _etlMount; 
                
            }
            set
            {
                if (_etlMount != value)
                {
                    _etlMount = value;
                    OnPropertyChanged("ETLMount");
                }
           
            }
        }

        [Category("4.调试")]
        [PropertyOrder(1)]
        [DisplayName("采样量")]
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
      
        [Category("4.调试")]
        [DisplayName("命令")]
        [PropertyOrder(3)]
        public ReadOnlyCollection<ICommand> Commands5
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {

                        new Command("单步调试", obj =>
                        {
                            ETLMount++;

                            var t = CurrentETLTools.Where(d => !(d is IDataExecutor) && d.Enabled).ToList();
                            if (ETLMount < t.Count)
                            {
                                XLogSys.Print.Info("插入ETL选项" + t[ETLMount].ToString());

                            }
                            RefreshSamples();
                        })
                    }
                    );
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

        [Category("2.清洗流程")]
        [DisplayName("已加载")] 
        public ObservableCollection<IColumnProcess> CurrentETLTools { get; set; }

        [Browsable(false)]
        public FrmState FrmState => FrmState.Large;

        [Browsable(false)]
        public virtual object UserControl => null;

        private void ExecuteAllExecutors()
        {
            if (MainDescription.IsUIForm &&
                ControlExtended.UserCheck("确定启动执行器？", "警告信息"))

            {
                ExecuteDatas();
            }
        }

        #endregion

        #region Public Methods

        public override void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(dicts, scenario);
            MaxThreadCount = dicts.Set("MaxThreadCount", MaxThreadCount);
            GenerateMode = dicts.Set("GenerateMode", GenerateMode);
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
                    }
                }
            }
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize(scenario);
            dict.Add("MaxThreadCount", MaxThreadCount);
            dict.Add("GenerateMode", GenerateMode);
            dict.Children = new List<FreeDocument>();
            dict.Children.AddRange(CurrentETLTools.Select(d => d.DictSerialize(scenario)));
            return dict;
        }
        
        public override bool Init()
        {
          
            RefreshSamples();
            CurrentETLTools.CollectionChanged += (s, e) =>
            {
                if (e.Action != NotifyCollectionChangedAction.Add) return;
                foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
                {
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
        private bool shouldUpdate = true;

     


        [Browsable(false)]
        public bool IsUISupport { get; set; }


        public void InitProcess(bool isexecute)
        {
            foreach (var item in CurrentETLTools.Where(d=>d.Enabled))
            {
                if (isexecute == false && item is IDataExecutor)
                {
                    continue;
                    
                }
                item.Init(new List<IFreeDocument>());
            }
        }

        private EnumerableFunc FuncAdd(IColumnProcess tool, EnumerableFunc func,bool isexecute)
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


            if (tool is IDataExecutor&& isexecute)
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


        public EnumerableFunc Aggregate(EnumerableFunc func, IEnumerable<IColumnProcess> tools,bool isexecute)
        {
            return tools.Aggregate(func, (current, tool) => FuncAdd(tool, current, isexecute));
        }


        public void ExecuteDatas()
        {
            var etls = CurrentETLTools.Take(ETLMount).Where(d=>d.Enabled).ToList();
            EnumerableFunc func = d => d;
            var index = 0;


            if (GenerateMode == GenerateMode.串行模式)
            {
                var generator = etls.FirstOrDefault() as IColumnGenerator;
                if (generator == null)
                    return;
                var realfunc3 = Aggregate(func, etls.Skip(1),true);
                var task = TemporaryTask.AddTempTask("串行ETL任务", generator.Generate(),
                    d => { realfunc3(new List<IFreeDocument>() {d}).ToList(); }, null, generator.GenerateCount() ?? (-1));
                SysProcessManager.CurrentProcessTasks.Add(task);

            }
            else
            {
                var timer = new DispatcherTimer();
                TemporaryTask paratask = null;
                var tolistTransformer = etls.FirstOrDefault(d => d.TypeName == "列表实例化") as ToListTF;
               
                if (tolistTransformer != null)
                {
                    index = etls.IndexOf(tolistTransformer);

                    var beforefunc = Aggregate(func, etls.Take(index),true);

                    paratask = TemporaryTask.AddTempTask("etl任务列表实例化", beforefunc(new List<IFreeDocument>())

                        ,
                        d2 =>
                        {
                            if (paratask.IsPause == false && SysProcessManager.CurrentProcessTasks.Count > MaxThreadCount)
                            {
                                iswait = true;
                                paratask.IsPause = true;
                            }
                            var countstr = d2.Query(tolistTransformer.MountColumn);
                            var name = d2.Query(tolistTransformer.IDColumn);
                            if (name == null)
                                name = "并行ETL任务";

                            int rcount = -1;
                            int.TryParse(countstr, out rcount);
                            var list = new List<IFreeDocument>() {d2};
                            var afterfunc = Aggregate(func, etls.Skip(index + 1), true);
                            var task = TemporaryTask.AddTempTask(name, afterfunc(list), d => { },
                                null, rcount, false);
                            if (tolistTransformer.DisplayProgress)
                                ControlExtended.UIInvoke(() => SysProcessManager.CurrentProcessTasks.Add(task));
                            task.Start();

                        }, d => timer.Stop(),-1,  false);
                
                }
                else
                {
                    var generator = etls.FirstOrDefault() as IColumnGenerator;
                    if (generator == null)
                        return;
                    var realfunc3 = Aggregate(func, etls.Skip(  1),true);
                    paratask = TemporaryTask.AddTempTask("并行ETL任务", generator.Generate(),
                        d =>
                        {
                            if (paratask.IsPause == false && SysProcessManager.CurrentProcessTasks.Count > MaxThreadCount)
                            {
                                iswait = true;
                                paratask.IsPause = true;

                            }
                            var task = TemporaryTask.AddTempTask("子任务", realfunc3(new List <IFreeDocument> { d }),
                                d2 => { },
                               null, 1, false);
                                ControlExtended.UIInvoke(() => SysProcessManager.CurrentProcessTasks.Add(task));
                            task.Start();

                        },d=>timer.Stop(),  generator.GenerateCount() ?? (-1), false);
                  
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

                    if (iswait == true && SysProcessManager.CurrentProcessTasks.Count < MaxThreadCount)
                     {
                         if (IsAutoSave)
                         {
                            SysProcessManager.CurrentProject.Save();
                         }
                            paratask.IsPause = false;
                            iswait = false;
                     }
                             


                };
            
                timer.Start();
            }
           
        }

        private bool iswait = false;
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
                    item.Column = p.Name;
                    ETLMount++;
                    InsertModule(item);
                        RefreshSamples();
                    
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


                window.Closed += (s, e) => RefreshSamples();
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
            return process.Name.ToLower().Contains(text) || process.Description.ToLower().Contains(text);
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
        [Category("3.执行")]
        [DisplayName("工作模式")]
        public GenerateMode GenerateMode { get; set; }

        [PropertyOrder(4)]
        [Category("3.执行")]
        [DisplayName("自动保存任务")]
        public bool IsAutoSave { get; set; }

        [PropertyOrder(2)]
        [Category("3.执行")]
        [Description("在并行模式工作时，所承载的最大线程数")]
        [DisplayName("最大线程数")]
        public int MaxThreadCount { get; set; }


    

        private bool generateFloatGrid;
        private int _etlMount = 100;

        public IEnumerable<IFreeDocument> Generate(IEnumerable<IColumnProcess> processes, bool isexecute ,IEnumerable<IFreeDocument>source =null )

        {
            if (source == null)
                source = new List<IFreeDocument>();
            var func = Aggregate(d => d, processes,isexecute);
            return func(source);

        } 
        public void RefreshSamples()
        {
            if (SysProcessManager == null)
                return;
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
                    alltoolList = dy.ETLToolList;
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
            Documents.Clear();
           
                var alltools = CurrentETLTools.Take(ETLMount).ToList();
            bool hasInit = false;
            var func=  Aggregate(d=>d, alltools.Where(d=>d.Enabled) ,false);
            SysProcessManager.CurrentProcessTasks.Add(TemporaryTask.AddTempTask("正在转换数据", func(new List<IFreeDocument>()).Take(SampleMount),
                data =>
                {
                    ControlExtended.UIInvoke(() => {
                        Documents.Add((data));
                        if (hasInit == false && Documents.Count > 2)
                        {
                            InitUI();
                            hasInit = true;
                        }
                    });
                  

                }, d =>
                {
                    if (!hasInit)
                    {
                        InitUI();
                        hasInit = true;
                    }
                       
                }
                , SampleMount));
        }

        private void InitUI()

        {
            var alltools = CurrentETLTools.Take(ETLMount).ToList();
           
                if (generateFloatGrid)
                {
                    var gridview = PluginProvider.GetObjectInstance<IDataViewer>("可编辑列表");

                    var r = gridview.SetCurrentView(Documents);

                    if (ControlExtended.DockableManager == null)
                        return;

                    ControlExtended.DockableManager.AddDockAbleContent(
                        FrmState.Custom, r, "样例数据");
                    generateFloatGrid = false;
                }
                else
                {
                    var view = new GridView();




                    Dict.Clear();
                    var keys = new List<string> { "" };
                    var docKeys = Documents.GetKeys(null, SampleMount);

                    keys.AddRange(docKeys);


                    foreach (var key in keys)
                    {
                        var col = new GridViewColumn
                        {
                            Header = key,
                            DisplayMemberBinding = new Binding($"[{key}]"),
                            Width = 155
                        };
                        view.Columns.Add(col);

                        var group = new SmartGroup
                        {
                            Name = key,
                            Value = alltools.Where(d => d.Column == key).ToList()
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
                        Dict.Add(group
                            );
                    }

                    var nullgroup = Dict.FirstOrDefault(d => string.IsNullOrEmpty(d.Name));
                    nullgroup?.Value.AddRange(
                        alltools.Where(
                            d =>
                                Documents.GetKeys().Contains(d.Column) == false &&
                                string.IsNullOrEmpty(d.Column) == false));
                    if (MainDescription.IsUIForm && IsUISupport)
                    {
                        if (dataView != null)
                            dataView.View = view;
                    }
                
            }
        }
        #endregion
    }

    public class SmartGroup : PropertyChangeNotifier
    {
        private string _name;

        #region Properties

        public ColumnInfo ColumnInfo { get; set; }
        public int Index { get; set; }

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

        public List<IColumnProcess> Value { get; set; }

        #endregion
    }

    public class GroupConverter : IValueConverter
    {
        public static Dictionary<string, string> map = new Dictionary<string, string>
        {
            {"IColumnDataSorter", "排序"},
            {"IColumnAdviser", "顾问"},
            {"IColumnGenerator", "生成"},
            {"IColumnDataFilter", "过滤"},
            {"IColumnDataTransformer", "转换"},
            {"IDataExecutor", "执行"}
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as XFrmWorkAttribute;

            if (s == null)
                return "未知";
            var p = s.MyType;
            foreach (var item in map)
            {
                if (p.GetInterface(item.Key) != null)
                    return item.Value;
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}