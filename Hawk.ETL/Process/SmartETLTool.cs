using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Executor;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Plugins.Transformers;
using Markdown.Xaml;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;

namespace Hawk.ETL.Process
{
    [XFrmWork("smartetl_name", "SmartETLTool_desc", "diagram", "数据采集和处理")]
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
            MaxThreadCount = 5;
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
                ETLToolsView.SortDescriptions.Add(new SortDescription("GroupName", ListSortDirection.Ascending));
                ETLToolsView.CustomSort = new NameComparer();
            }
        }

        #endregion
        [Browsable(false)]
        public ConfigFile Config => ConfigFile.GetConfig<DataMiningConfig>();

        #region Constants and Fields

        private ListBox alltoolList;
        [Browsable(false)]
        public Analyzer Analyzer { get; set; }
        private DataGrid dataView;

        private ListView currentToolList;
        private ScrollViewer scrollViewer;
        private string searchText = GlobalHelper.Get("key_110");

        #endregion

        #region Properties
        [Browsable(false)]
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


        private IEnumerable<ToolBase> GetSelectedTools(object data = null)
        {
            if (currentToolList == null)
                yield break;
            if (data == null)
            {
                foreach (var col in currentToolList.SelectedItems.IListConvert<ToolBase>())
                {
                    yield return col;
                }
                yield break;
            }

            if (data is ToolBase)
            {
                yield return data as ToolBase;
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
                        new Command(GlobalHelper.Get("key_631"), obj => DropAction("Click", obj), obj => obj != null,
                            "settings"),
                        new Command(GlobalHelper.Get("key_679"), obj => DropAction("Delete", obj), obj => obj != null,
                            "delete"),
                        new Command(GlobalHelper.Get("clear_tool"), obj =>
                        {
                            var item = obj as SmartGroup;
                            foreach (var ColumnProcess in item.Value)
                                CurrentETLTools.Remove(ColumnProcess);
                            RefreshSamples();
                        }, obj => obj != null, "clear"),
                        new Command(GlobalHelper.Get("key_681"), obj =>
                        {
                            var tools = GetSelectedTools().ToList();
                            Clipboard.SetDataObject(
                                FileConnector.GetCollectionString(tools.Select(d => d.DictSerialize()).ToList()), false);
                            XLogSys.Print.Warn(GlobalHelper.Get("cliptoboard"));
                        }, obj => obj != null, "clipboard_file"),
                        new Command(GlobalHelper.Get("cut_tools"), obj =>
                        {
                            var tools = GetSelectedTools().ToList();
                            CurrentETLTools.RemoveElementsNoReturn(d => tools.Contains(d));
                            Clipboard.SetDataObject(
                                FileConnector.GetCollectionString(tools.Select(d => d.DictSerialize()).ToList()), false);
                            ;
                            XLogSys.Print.Warn(GlobalHelper.Get("cliptoboard"));
                        }, obj => obj != null, "clipboard_file"),
                        new Command(GlobalHelper.Get("clip_up"), obj =>
                        {
                            var item = GetSelectedTools(obj).FirstOrDefault();
                            var index = CurrentETLTools.IndexOf(item);
                            ControlExtended.SafeInvoke(() =>
                            {
                                var data = Clipboard.GetDataObject();
                                var toolsConfig = (string) data.GetData(typeof (string));
                                var toolsDoc = FileConnectorXML.GetCollection(toolsConfig);
                                var tools = toolsDoc.Select(GetToolFromDocument);
                                foreach (var tool in tools)
                                {
                                    CurrentETLTools.Insert(index++, tool);
                                }
                            });
                        }, obj => Clipboard.GetDataObject().GetFormats().Length != 0, "arrow_up"),
                        new Command(GlobalHelper.Get("clip_down"), obj =>
                        {
                            var item = GetSelectedTools(obj).FirstOrDefault();
                            var index = CurrentETLTools.IndexOf(item) + 1;
                            ControlExtended.SafeInvoke(() =>
                            {
                                var data = Clipboard.GetDataObject();
                                var toolsConfig = (string) data.GetData(typeof (string));
                                var toolsDoc = FileConnectorXML.GetCollection(toolsConfig);
                                var tools = toolsDoc.Select(d => GetToolFromDocument(d));
                                foreach (var tool in tools)
                                {
                                    CurrentETLTools.Insert(index++, tool);
                                }
                            });
                        }, obj => Clipboard.GetDataObject().GetFormats().Length != 0, "arrow_down"),
                        new Command(GlobalHelper.Get("key_684"),
                            obj => { ETLMount = CurrentETLTools.IndexOf(obj as IColumnProcess); },
                            obj => obj != null, "tag"),
                        new Command(GlobalHelper.Get("key_685"), obj =>
                        {
                            var index = CurrentETLTools.IndexOf(obj as IColumnProcess);
                            CurrentETLTools.KeepRange(0, index + 1);
                            ETLMount = index + 1;
                        },
                            obj => obj != null, "tag"),
                        new Command(GlobalHelper.Get("doc_etl_read"), obj =>
                        {
                            var doc = this.GenerateRemark(true, SysProcessManager);
                            var docItem= new DocumentItem() {Title = this.Name,Document = doc};
                            var window=PropertyGridFactory.GetPropertyWindow(docItem);
                            window.Title = GlobalHelper.Get("key_267");
                            window.ShowDialog();
                        },
                            obj => this.CurrentETLTools.Count>0, "question"),
                         new Command(GlobalHelper.Get("move_up"), obj =>
                        {
                            var item = GetSelectedTools(obj).FirstOrDefault();
                            var index = CurrentETLTools.IndexOf(item);
                            CurrentETLTools.Move(index,index-1);
                            ETLMount = index + 1;
                        },
                            obj => true, "arrow_up"),

                          new Command(GlobalHelper.Get("move_down"), obj =>
                        {
                           var item = GetSelectedTools(obj).FirstOrDefault();
                            var index = CurrentETLTools.IndexOf(item);
                            CurrentETLTools.Move(index,index+1);
                            ETLMount = index + 1;
                        },
                            obj => true, "arrow_down"),


                    });
            }
        }

        private void InsertModule(IColumnProcess tool)
        {
            if (ETLMount < 0 || ETLMount >= CurrentETLTools.Count)
                CurrentETLTools.Add(tool);
            else
                CurrentETLTools.Insert(ETLMount, tool);
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
                                XLogSys.Print.Info(GlobalHelper.Get("key_692") + CurrentTool?.ToString());
                        }, obj => ETLMount < CurrentETLTools.Count, "arrow_right"),
                        new Command(GlobalHelper.Get("key_693"), obj => { ETLMount = 0; }, icon: "align_left"),
                        new Command(GlobalHelper.Get("jump_last"), obj => { ETLMount = CurrentETLTools.Count; },
                            icon: "align_right"),
                        new Command(GlobalHelper.Get("key_695"), obj => { EnterAnalyzer(); }, icon: "magnify_add")
                    }
                    );
            }
        }

        [Browsable(false)]
        public ReadOnlyCollection<ICommand> CommandsListView
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("paste_tools"), obj =>
                        {
                            var data = Clipboard.GetDataObject();
                            var toolsConfig = (string) data.GetData(typeof (string));
                            var toolsDoc = FileConnectorXML.GetCollection(toolsConfig);
                            var tools = toolsDoc.Select(GetToolFromDocument);
                            foreach (var tool in tools)
                            {
                                CurrentETLTools.Add(tool);
                            }
                        }, icon: "clipboard")
                    }
                    );
            }
        }
        [PropertyEditor("CodeEditor")]
        [PropertyOrder(100)]
        [LocalizedDisplayName("remark")]
        [LocalizedDescription("remark_desc")]
        public string Remark { get; set; }

        private void EnterAnalyzer()
        {
            var view = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("key_696")) as UserControl;
            view.DataContext = Analyzer;

            ControlExtended.DockableManager.AddDockAbleContent(
                FrmState.Custom, view, GlobalHelper.Get("debugview"));
        }

        private WPFPropertyGrid debugGrid;

        [Browsable(false)]
        public IColumnProcess CurrentTool
        {
            get
            {
                var t = CurrentETLTools; //.Where(d => !(d is IDataExecutor) && d.Enabled).ToList();
                IColumnProcess current = null;
                if (ETLMount <= t.Count && ETLMount > 1)
                    current = t[ETLMount - 1];
                if (DisplayDetail)
                {
                    if (debugGrid == null)
                        debugGrid = PropertyGridFactory.GetInstance(current);
                    else
                        debugGrid.SetObjectView(current);
                    dynamic control =
                        (MainFrm as IDockableManager).ViewDictionary.FirstOrDefault(d => d.View == debugGrid)
                            ?.Container;
                    if (control != null)
                        control.Show();

                    else
                        (MainFrm as IDockableManager).AddDockAbleContent(FrmState.Float, debugGrid,
                            GlobalHelper.Get("key_698"));
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
                //var t = CurrentETLTools.Where(d => !(d is IDataExecutor) && d.Enabled).ToList();
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
                    ETLToolsView.Filter = FilterMethod;
                OnPropertyChanged("SearchText");
            }
        }

        [Browsable(false)]
        protected List<XFrmWorkAttribute> AllETLTools { get; set; }


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

                ExecuteDatas();
        }

        #endregion

        #region Public Methods

        private ToolBase GetToolFromDocument(FreeDocument child)
        {
            var name = child["Type"].ToString();
            var process = PluginProvider.GetObjectByType<IColumnProcess>(name);
            if (process != null)
            {
                process.DictDeserialize(child);

                process.Father = this;
                var tool = process as ToolBase;
                if (tool != null)
                    tool.ColumnSelector.GetItems = () => all_columns;
                return tool;
            }
            return null;
        }

        public override void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            shouldUpdate = false;
            base.DictDeserialize(dicts, scenario);
            MaxThreadCount = dicts.Set("MaxThreadCount", MaxThreadCount);
            Remark = dicts.Set("Remark", Remark);
            object generatemode = null;

            if (dicts.TryGetValue("GenerateMode", out generatemode))
            {
                if (generatemode.ToString() == "串行模式")
                    GenerateMode = GenerateMode.SerialMode;
                else if (generatemode.ToString() == "并行模式")
                    GenerateMode = GenerateMode.ParallelMode;
                else
                {
                    GenerateMode = dicts.Set("GenerateMode", GenerateMode);
                }
            }

            DelayTime = dicts.Set("DelayTime", DelayTime);
            SampleMount = dicts.Set("SampleMount", SampleMount);
            var doc = dicts as FreeDocument;
            if (doc != null && doc.Children != null)
                foreach (var child in doc.Children)
                {
                    var tool = GetToolFromDocument(child);
                    if(string.IsNullOrEmpty(tool.ObjectID))
                        tool.ObjectID = string.Format("{0}_{1}_{2}", tool.TypeName, tool.Column, CurrentETLTools.Count);
                    CurrentETLTools.Add(tool);
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
            dict.Add("DelayTime", DelayTime);
            dict.Add("Remark", Remark);
            dict.Add(FreeDocument.KeepOrder, true);
            dict.Children = new List<FreeDocument>();
            dict.Children.AddRange(CurrentETLTools.Select(d => d.DictSerialize(scenario)));
            return dict;
        }

        public override bool Init()
        {
            mudoleHasInit = true;
            Analyzer.DataManager = SysDataManager;
            RefreshSamples();
            CurrentETLTools.CollectionChanged += (s, e) =>
            {
                if (e.Action != NotifyCollectionChangedAction.Add) return;
                var canFresh = false;
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
                        if (e2.PropertyName == "AnalyzeItem")
                            return;
                        canFresh = true;
                    };
                }
                if (canFresh) RefreshSamples();
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
                    continue;
                item.Init(new List<IFreeDocument>());
            }
        }

        [LocalizedCategory("key_199")]
        [LocalizedDisplayName("key_200")]
        public override string Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;
                if (mudoleHasInit && MainDescription.IsUIForm && string.IsNullOrEmpty(_name) == false &&
                    string.IsNullOrEmpty(value) == false)
                {
                    var dock = MainFrm as IDockableManager;
                    var view = dock?.ViewDictionary.FirstOrDefault(d => d.Model == this);
                    if (view != null)
                    {
                        dynamic container = view.Container;
                        container.Title = _name;
                    }
                    var oldtools =
                        SysProcessManager.CurrentProcessCollections.OfType<SmartETLTool>()
                            .SelectMany(d => d.CurrentETLTools)
                            .OfType<ETLBase>()
                            .Where(d => d.ETLSelector.SelectItem == _name)
                            .ToList();

                    if (oldtools.Count > 0)
                    {
                        var res = MessageBox.Show(string.Format(GlobalHelper.Get("check_if_rename"), TypeName,
                            _name, value,
                            string.Join(",", oldtools.Select(d => d.ObjectID)), ""), GlobalHelper.Get("Tips"),
                            MessageBoxButton.YesNo);

                        if (res == MessageBoxResult.Yes)
                        {
                            oldtools.Execute(d => d.ETLSelector.SelectItem = value);
                        }
                    }
                }
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        private TemporaryTask<IFreeDocument> AddSubTask(List<IFreeDocument> seeds, EnumerableFunc mapperFunc1, EnumerableFunc mapperFunc2,EnumerableFunc customerFunc3,
            ToListTF motherListTF = null,
            TemporaryTask<IFreeDocument> lastTask = null, string name = null, bool isAdd = true)
        {
          
        
            if (lastTask != null)
            {
                seeds = lastTask.Seeds?.ToList();
                name = lastTask.Name;
            }
            if (seeds == null)
                seeds = new List<IFreeDocument>();

            var realCount = -1;
            if (motherListTF != null)
            {
                var d2 = seeds.FirstOrDefault();
                name = d2[motherListTF.Column]?.ToString();
                if (name == null)
                    name = motherListTF.Column;
                int.TryParse(d2.Query(motherListTF.MountColumn), out realCount);
            }
            var mapperIndex1 = lastTask?.MapperIndex1 ?? 0;
            var mapperIndex2 = lastTask?.MapperIndex2 ?? 0;
            var task = new TemporaryTask<IFreeDocument>();
            if  (String.IsNullOrEmpty(name))
                name = GlobalHelper.Get("key_706");
            task.Name = name;
            if (lastTask != null)
                task.IsPause = true;
            task.Seeds = seeds.Select(d => d.Clone()).ToList();
            var task1 = task;
            var generator1 = mapperFunc1(seeds).Skip(mapperIndex1).Select(doc =>
            {
                task1.MapperIndex1++;
                task1.MapperIndex2 = 0;
                return doc;
            });
            var generator2 = mapperFunc2(generator1).Skip(mapperIndex2).Select(doc =>
            {
                task1.MapperIndex2++;
                return doc;
            });
            task = TemporaryTask<IFreeDocument>.AddTempTask(task, generator2, doc =>
            {
                var list = new List<IFreeDocument> {doc};
                return customerFunc3(list);
            },
                null, realCount, false);
            if (lastTask != null)
            {
                task.Name = lastTask.Name;
                task.OutputIndex = lastTask.OutputIndex;
                task.MapperIndex1 = lastTask.MapperIndex1;
                task.MapperIndex2 = lastTask.MapperIndex2;
                task.IsPause = true;
            }
            task.IsFormal = true;
            task.Level = 1;
            task.Publisher = this;
            if (isAdd)
            {
                ControlExtended.UIInvoke(() => SysProcessManager.CurrentProcessTasks.Add(task));
            }

            task.Start();
            return task;
        }
        
        private int maxThreadCount
        {
            get { return GenerateMode == GenerateMode.SerialMode ? 1 : MaxThreadCount; }
        }

        public void ExecuteDatas(List<TemporaryTask<IFreeDocument>> lastRunningTasks = null)
        {
            var etls = CurrentETLTools.Where(d => d.Enabled).ToList();
            SysProcessManager.CurrentProject.Build();

            Analyzer.Start(Name);

            var timer = new DispatcherTimer();
            if (GenerateMode == GenerateMode.SerialMode && DelayTime > 0)
                etls = etls.AddModule(d => d.GetType() == typeof(CrawlerTF),
                    d => new DelayTF { DelayTime = DelayTime.ToString() }, true).ToList();
            ToListTF motherListTF;

            var taskBuff = new List<IFreeDocument>();
            TemporaryTask<IFreeDocument> motherTask = null;
            TemporaryTask<IFreeDocument> lastMotherTask = null;

            var motherName = Name + GlobalHelper.Get(GenerateMode == GenerateMode.SerialMode ? "key_704" : "key_705");
            if (lastRunningTasks != null)
                lastMotherTask = lastRunningTasks.FirstOrDefault(d => d.Level == 0);


            var mapperIndex1 = lastMotherTask?.MapperIndex1 + 1 ?? 0;
            var splitPoint = etls.GetParallelPoint(false, out motherListTF);
            var motherFunc = etls.Take(splitPoint).Aggregate(isexecute: true, analyzer: Analyzer);
            if (motherListTF != null)
                splitPoint++;
            var subEtls = etls.Skip(splitPoint).ToList();
            motherTask = new TemporaryTask<IFreeDocument>();
            motherTask.Name = motherName;
            if (lastMotherTask != null)
                motherTask.IsPause = true;

            ToListTF subTaskToListTf;
            ToListTF subTaskToListTf2;

            var splitPoint1 = subEtls.GetParallelPoint(false, out subTaskToListTf);
            var mapperFunc1 = subEtls.Take(splitPoint1).Aggregate(isexecute: true, analyzer: Analyzer);
            if (subTaskToListTf != null)
                splitPoint1++;

            var subEtls2 = subEtls.Skip(splitPoint1).ToList();
            var splitPoint2 = subEtls2.GetParallelPoint(false, out subTaskToListTf2);


            var mapperFunc2 = subEtls2.Take(splitPoint2).Aggregate(isexecute: true, analyzer: Analyzer);
            if (subTaskToListTf != null)
                splitPoint2++;
            var customerFunc3 = subEtls2.Skip(splitPoint2).Aggregate(isexecute: true, analyzer: Analyzer);




            TemporaryTask<IFreeDocument>.AddTempTaskSimple(motherTask
                ,
                motherFunc(new List<IFreeDocument>()).Skip(mapperIndex1).Select(d =>
                {
                    motherTask.MapperIndex1++;
                    if (this.SysProcessManager.CurrentProcessTasks.Contains(motherTask) == false)
                    {
                        motherTask.Remove();

                    }
                    PauseCheck(motherTask);
                    var delay = this.DelayTime;
                    if (GenerateMode == GenerateMode.ParallelMode)
                        delay = 0;
                    Thread.Sleep(Math.Max(100, delay));
                    return d;
                }),
                d =>
                {
                taskBuff.Add(d);
                if (taskBuff.Count < motherListTF?.GroupMount)
                {
                    return;
                }
                    PauseCheck(motherTask);

                    AddSubTask(taskBuff.ToList(), mapperFunc1,mapperFunc2,customerFunc3,  motherListTF);
                    taskBuff.Clear();
                });
            if (lastRunningTasks != null)
                foreach (var subTask in lastRunningTasks.Where(d => d.Level == 1))
                    AddSubTask(null, mapperFunc1, mapperFunc2, customerFunc3, 
                        motherListTF, subTask);
            SysProcessManager.CurrentProcessTasks.Add(motherTask);
            motherTask.IsFormal = true;

            if (lastMotherTask != null)
            {
                motherTask.IsPause = true;

                motherTask.MapperIndex1 = lastMotherTask.MapperIndex1;
                motherTask.OutputIndex = lastMotherTask.OutputIndex;
                AttachTask(motherTask);
            }
            motherTask.Level = 0;
            motherTask.Publisher = this;
            timer.Interval = TimeSpan.FromMilliseconds(100);
        

            timer.Tick += (s, e) =>
            {
                if (motherTask.IsCanceled)
                {
                    timer.Stop();
                    return;
                }

                if (motherTask.IsStart == false)
                {
                    motherTask.Start();
                    return;
                }

                PauseCheck(motherTask,false);
            };

            timer.Start();
        }

        private void PauseCheck(TaskBase motherTask, bool check = true)
        {
            if (motherTask.ShouldPause == false)

                motherTask.IsPause = SysProcessManager.CurrentProcessTasks.OfType<TemporaryTask<IFreeDocument>>().Count(d2 => d2.Publisher == this&&d2.Level==1) >= maxThreadCount;
            if(check)

                motherTask.CheckWait();

        }
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
                    item.ObjectID = string.Format("{0}_{1}_{2}", item.TypeName, item.Column, CurrentETLTools.Count);
                    if (NeedConfig(item))
                    {
                        PropertyGridFactory.GetPropertyWindow(item).ShowDialog();
                    }
                    ETLMount++;
                }
            }
            if (sender == "Click")
            {
                var smart = attr as SmartGroup;
                if (smart != null)
                    attr = smart.ColumnInfo;
                var window = PropertyGridFactory.GetPropertyWindow(attr);
                var oldProp = attr.UnsafeDictSerializePlus();

                window.Closed += (s, e) =>
                {
                    if (oldProp.IsEqual(attr.UnsafeDictSerializePlus()) == false && IsAutoRefresh)
                    {
                        (attr as PropertyChangeNotifier).OnPropertyChanged("");
                    }
                };
                window.ShowDialog();
            }
            if (sender != "Delete") return true;
            var tools = GetSelectedTools().ToList();
            if (tools.Count > 0 &&
                MessageBox.Show(
                    string.Format(GlobalHelper.Get("key_708"), string.Join(" ", tools.Select(d => d.TypeName))),
                    GlobalHelper.Get("key_99"),
                    MessageBoxButton.OKCancel) !=
                MessageBoxResult.OK) return true;

            CurrentETLTools.RemoveElementsNoReturn(d => tools.Contains(d));
            ETLMount = CurrentETLTools.Count;

            return true;
        }


        private bool FilterMethod(object obj)
        {
            var process = obj as XFrmWorkAttribute;
            if (process == null)
                return false;
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

        [Browsable(false)]
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
                    if (value > 20)
                    {
                        value = 20;
                        XLogSys.Print.Warn(GlobalHelper.Get("key_711"));
                    }
                    if (value <= 0)
                        value = 1;
                    _maxThreadCount = value;
                    OnPropertyChanged("MaxThreadCount");
                }
            }
        }
        [Browsable(false)]
        [LocalizedDescription("key_709")]
        [LocalizedDisplayName("key_395")]
        [NumberRange(1, 20, 1)]
        public int DelayTime
        {
            get { return _delayTime; }
            set
            {
                if (_delayTime != value)
                {
                    _delayTime = value;
                    OnPropertyChanged("DelayTime");
                }
            }
        }

        private int _etlMount;


        private bool mudoleHasInit;
        private int _maxThreadCount;
        private GenerateMode _generateMode;
        private bool isErrorRemind = true;
        private readonly List<string> all_columns = new List<string>();
        private bool _isAutoRefresh;
        private DateTime lastRefreshTime;
        private int _delayTime;

        public void RefreshSamples(bool canGetDatas = true)
        {
            if (shouldUpdate == false)
                return;

            if (SysProcessManager == null)
                return;
            if (!mudoleHasInit)
                return;
            OnPropertyChanged("AllETLMount");
            SysProcessManager.CurrentProject.Build();
            var tasks =
                SysProcessManager.CurrentProcessTasks.Where(d => d.Publisher == this && d.IsPause == false).ToList();
            if (tasks.Any())
            {
                var str = string.Format(GlobalHelper.Get("task_run"), Name);
                if (isErrorRemind == false)
                {
                    XLogSys.Print.Warn(string.Format(GlobalHelper.Get("key_712"), Name));
                    return;
                }
                if (!MainDescription.IsUIForm)
                    return;
                var result =
                    MessageBox.Show(str, GlobalHelper.Get("key_99"), MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    foreach (var item in tasks)
                        item.Remove();
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
                            return;
                        var process = currentToolList.SelectedValue as IColumnProcess;
                        if (process == null)
                            return;
                        var oldProp = process.UnsafeDictSerializePlus();
                        var window = PropertyGridFactory.GetPropertyWindow(process);
                        window.Closed += (s2, e2) =>
                        {
                            if (
                                (oldProp.IsEqual(process.UnsafeDictSerializePlus()) == false && IsAutoRefresh).SafeCheck
                                    (GlobalHelper.Get("key_714"), LogType.Debug))
                                RefreshSamples();
                            (process as PropertyChangeNotifier).OnPropertyChanged("");
                        };
                        window.ShowDialog();
                    };

                    alltoolList.MouseMove += (s, e) =>
                    {
                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            var attr = alltoolList.SelectedItem as XFrmWorkAttribute;
                            if (attr == null)
                                return;

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
            var func = alltools.Aggregate(isexecute: false, analyzer: Analyzer);
            if (!canGetDatas)
                return;
            SmartGroupCollection.Clear();
            Documents.Clear();

            shouldUpdate = true;
            if (!MainDescription.IsUIForm)
                return;
            all_columns.Clear();
            dataView.Columns.Clear();

            AddColumn("", alltools);
            var temptask = TemporaryTask<FreeDocument>.AddTempTaskSimple(Name + "_" + GlobalHelper.Get("key_108"),
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

                        Documents.Add(data);
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
                                var target = etl.GetTask<SmartETLTool>(etl.ETLSelector.SelectItem);
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
                            scrollViewer.ScrollToHorizontalOffset(index*CellWidth);
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
            AttachTask(temptask);

            dynamic tempwindow = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("etl_temp_window"));
            SysProcessManager.CurrentProcessTasks.Add(temptask);
            temptask.PropertyChanged += (s, e) =>
            {
                if (temptask.IsCanceled == true || temptask.IsPause)
                {
                    tempwindow.Close();
                }
             
            };

            tempwindow.DataContext = tempwindow;
            tempwindow.BindingSource = temptask;
           
            tempwindow.ShowDialog();
            
            if (tempwindow.DialogResult == false)
            {
             
                temptask.Remove();
                if (tempwindow.Refresh)
                {
                        RefreshSamples(true);
                }
            }
          
          
         
        }

        private void AttachTask(TemporaryTask<IFreeDocument> temptask)
        {
            temptask.PropertyChanged += (s, e) =>
            {
                ControlExtended.UIInvoke(() =>
                {
                    var dock = MainFrm as IDockableManager;
                    switch (e.PropertyName)
                    {
                        case "Percent":

                            dock.SetBusy(ProgressBarState.Normal, message: GlobalHelper.Get("long_etl_task"),
                                percent: temptask.Percent);
                            break;
                        case "IsStart":

                            if (temptask.IsStart == false)
                                dock.SetBusy(ProgressBarState.NoProgress);
                            else
                            {
                                if (temptask.Total < 0)
                                    dock.SetBusy(ProgressBarState.Indeterminate);
                            }
                            break;
                        default:
                            break;
                    }
                });
            };
        }

        public static int CellWidth = 155;

        private void DeleteColumn(string key)
        {
            SmartGroupCollection.RemoveElementsNoReturn(d => d.Name == key);
            dataView.Columns.RemoveElementsNoReturn(d => d.Header.ToString() == key);
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

            binding.Path = new PropertyPath($"[{key}]");
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
                return 1;
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