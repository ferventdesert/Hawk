using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Connectors.Vitural;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;
using Microsoft.Win32;


namespace Hawk.ETL.Managements
{
    public class QueryEntity
    {
        public Action<List<FreeDocument>> GetQueryFunc;
        public List<ICommand> commands;

        public QueryEntity()
        {
            commands = new List<ICommand>();
            commands.Add(new Command("执行查询", async obj =>
            {
                List<FreeDocument> datas = null;
                try
                {
                    ControlExtended.SetBusy(true);
                    int count;
                    datas = await Task.Run(() => Connector.QueryEntities(SQL,
                        out count, TableInfo == null ? null : TableInfo.Name));
                    ControlExtended.SetBusy(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("查询出现异常" + ex.Message);
                    XLogSys.Print.Error(ex);
                    return;
                }
                finally
                {
                    ControlExtended.SetBusy(false);
                }
                GetQueryFunc(datas);
            }, obj => string.IsNullOrEmpty(SQL) == false));
        }

        [LocalizedDisplayName("当前表")]
        [PropertyOrder(1)]
        public TableInfo TableInfo { get; set; }

        [LocalizedDisplayName("当前连接")]
        [PropertyOrder(0)]
        public IDataBaseConnector Connector { get; set; }

        [LocalizedDisplayName("查询字符串")]
        [LocalizedDescription("根据数据库的不同，可在此处输入JS（MongoDB）和标准SQL")]
        [StringEditor("SQL")]
        [PropertyOrder(2)]
        [PropertyEditor("DynamicScriptEditor")]
        public string SQL { get; set; }

        [PropertyOrder(3)]
        [LocalizedDisplayName("执行")]
        public ReadOnlyCollection<ICommand> Commands => new ReadOnlyCollection<ICommand>(commands);
    }

    [XFrmWork("数据管理",  "查看和分析数据", "")]
    public class DataManager : AbstractPlugIn, IDataManager, IView
    {
        #region Constants and Fields



        private IDockableManager dockableManager;


        private XFrmWorkPropertyGrid propertyGridWindow;



        #endregion

        #region Events

        public ICollection<IDataBaseConnector> CurrentConnectors => _dbConnections;

        public event EventHandler DataSourceChanged;

        #endregion

        #region Properties

        private List<ICommand> commands;

        public ReadOnlyCollection<ICommand> Commands => new ReadOnlyCollection<ICommand>(commands);

        public WPFPropertyGrid ConfigUI { get; set; }


        private ObservableCollection<IDataBaseConnector> _dbConnections;

        public string NewTableName { get; set; }

        public string SQLQuery { get; set; }


        public ICollection<DataCollection> DataCollections { get; set; }


        public IEnumerable<string> DataNameCollection
        {
            get
            {
                return DataCollections?.Select(i => i.Name);
            }
        }


        public FrmState FrmState => FrmState.Mini;

        public object UserControl => null;

        #endregion

        #region Public Methods

        private IProcessManager processManager;
        public DataCollection SelectedDataCollection { get; set; }

        public DataCollection ReadFile(string fileName, string format = null)
        {
            IFileConnector exporter = null;
            if (format != null)
            {
                exporter = PluginProvider.GetObjectInstance<IFileConnector>(format);
            }
            else
            {
                exporter = FileConnector.SmartGetExport(fileName);
            }

            if (exporter == null)
            {
                return null;
            }
            exporter.FileName = fileName;


            fileName = exporter.FileName;

            ControlExtended.SafeInvoke(
                () => AddDataCollection(exporter.ReadFile(), Path.GetFileNameWithoutExtension(fileName)),LogType.Important);
            return GetCollection(fileName);
        }

        public DataCollection ReadCollection(IDataBaseConnector connector, string tableName, bool isVirtual)
        {
            if (isVirtual)
            {
                IItemsProvider<IFreeDocument> vir = null;
                TableInfo tableInfo = connector.RefreshTableNames().FirstOrDefault(d => d.Name == tableName);

                var enumable = connector as IEnumerableProvider<IFreeDocument>;
                if (enumable != null && enumable.CanSkip(tableInfo.Name) == false)
                {
                    vir = new EnumableVirtualProvider<IFreeDocument>(
                        enumable.GetEnumerable(tableInfo.Name), tableInfo.Size);
                }
                else
                {
                    vir = new DataBaseVirtualProvider<IFreeDocument>(tableInfo.Connector, tableInfo.Name);
                }
                int count = 1000;
                if (connector.TypeName == "网页爬虫连接器")
                    count = 100;
                var col = new VirtualDataCollection( vir, count)
                {
                    Name = tableInfo.Name
                };
                AddDataCollection(col);
                return col;
            }
            else
            {
                Task<List<IFreeDocument>> datas = GetDataFromDB(connector, tableName, true);
                DataCollection col = AddDataCollection(datas.Result);
                return col;
            }
        }


        public async Task<List<IFreeDocument>> GetDataFromDB(IDataBaseConnector db, string dataName,
            bool isNewData, int mount = -1)
        {
            if (db == null)
            {
                return null;
            }

            var table = db.RefreshTableNames().FirstOrDefault(d => d.Name == dataName);
            var dataAll = new List<IFreeDocument>();
            
                var  task = TemporaryTask.AddTempTask(dataName + "数据导入",
                 db.GetEntities(dataName, mount), dataAll.Add,null,table!=null?table.Size:-1,notifyInterval:1000);
            processManager.CurrentProcessTasks.Add(task);
                await Task.Run(
                    () => task.Wait());
                return dataAll;
        }

        private DataCollection GetCollection(object data)
        {
            if (data == null)
            {
                if (SelectedDataCollection != null) return SelectedDataCollection;
            }

            if (data is DataCollection)
            {
                return data as DataCollection;
            }
            return null;
        }


        private string GetNewName(string name = "新建数据集")
        {
            if (DataCollections.Any(d => d.Name == name))
            {
                char item =
                    name.Last();
                if (Char.IsDigit(item))
                {
                    int p = int.Parse(item.ToString());
                    name = name.Substring(name.Length - 2) + (p + 1);
                }
                else
                {
                    name += '1';
                }
            }
            return name;
        }

        public override bool Init()
        {
            if (MainDescription.IsUIForm)
            {
                DataCollections = new SafeObservable<DataCollection>();
                dockableManager = MainFrmUI as IDockableManager;

                var view = PluginProvider.GetObjectInstance<ICustomView>("系统状态视图");
                var userControl = view as UserControl;
                if (userControl != null)
                {
                    userControl.DataContext = MainFrmUI;
                    dockableManager.AddDockAbleContent(FrmState.Mini, view, "系统状态视图");
                }
            }

            else
            {
                DataCollections = new ObservableCollection<DataCollection>();
            }

            processManager = MainFrmUI.PluginDictionary["模块管理"] as IProcessManager;


            commands = new List<ICommand>();
            var dbaction = new BindingAction();
            dbaction.ChildActions.Add(new Command("配置连接",
                obj =>
                {
               
                    Window w = PropertyGridFactory.GetPropertyWindow(obj);
                    w.ShowDialog();
                },
                obj => obj != null));
            dbaction.ChildActions.Add(
                new Command("刷新", obj => RefreshConnect(obj as IDataBaseConnector), obj => obj != null));
            dbaction.ChildActions.Add(
                new Command("执行查询", obj =>
                {
                    var query = new QueryEntity();
                    query.Connector = obj as IDataBaseConnector;
                    query.GetQueryFunc = d =>
                    {
                        if (d == null)
                            return;
                        if (d.Any() == false)
                            return;

                        AddDataCollection(d, GetNewName());
                    };
                    propertyGridWindow.SetObjectView(query);
                }, obj => obj != null));
            dbaction.ChildActions.Add(
                new Command("删除连接", obj =>
                {
                    if (MessageBox.Show("确定要删除该连接吗？", "提示信息", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                    {
                        var con = obj as DBConnectorBase;
                        _dbConnections.Remove(con);
                    }
                }, obj => obj != null));
            var dataaction = new BindingAction();


            var tableAction = new BindingAction();
            tableAction.ChildActions.Add(new Command(
                "查看",
                async obj =>
                {
                    var items = obj as TableInfo;
                    List<IFreeDocument> dataAll = null;
                    try
                    {
                        dataAll= await
                      GetDataFromDB(items.Connector, items.Name, true,
                          items.Connector is FileManager ? -1 : 200);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("文件打开失败" + ex.Message);
                        return;
                    }
                  
                 
                    if (dataAll == null || dataAll.Count == 0)
                    {
                        XLogSys.Print.Warn("没有在表中的发现可用的数据");
                        return;
                    }
                    if (items.Connector is FileManager)
                    {
                        string file = (items.Connector as FileManager).LastFileName;
                        var name = Path.GetFileNameWithoutExtension(file);
                        AddDataCollection(dataAll, name);
                        return;
                    }
                    var excel = PluginProvider.GetObjectInstance<IDataViewer>("可编辑列表");
                    if (excel == null)
                        return;
                    object view = excel.SetCurrentView(dataAll.Select(d=>d as IFreeDocument).ToList());

                    if (ControlExtended.DockableManager != null)
                    {
                        ControlExtended.DockableManager.AddDockAbleContent(
                            FrmState.Custom, view, items.Name);
                    }
                },
                obj => obj != null));
            tableAction.ChildActions.Add(new Command(
                "添加到数据集",
                async obj =>
                {
                    var items = obj as TableInfo;
                    List<IFreeDocument> datas = await GetDataFromDB(items.Connector, items.Name, true);
                    if (datas == null)
                        return;
                    AddDataCollection(datas, items.Name);
                },
                obj => obj != null));
            tableAction.ChildActions.Add(new Command(
                "添加虚拟数据集",
                obj =>
                {
                    var con = obj as TableInfo;
                    ReadCollection(con.Connector, con.Name, true);
                },
                obj => obj != null));
            tableAction.ChildActions.Add(new Command(
                "删除表",
                obj =>
                {
                    var items = obj as TableInfo;

                    DropTable(items.Connector, items.Name);
                },
                obj => obj != null));
            tableAction.ChildActions.Add(new Command(
                "查看属性",
                obj =>
                {
                    Window w = PropertyGridFactory.GetPropertyWindow(obj);
                    w.ShowDialog();
                },
                obj => obj != null));
            tableAction.ChildActions.Add(new Command(
                "执行查询",
                obj =>
                {
                    var query = new QueryEntity();
                    query.TableInfo = obj as TableInfo;
                    query.Connector = query.TableInfo.Connector;
                    query.GetQueryFunc = d =>
                    {
                        if (d == null)
                            return;
                        if (d.Any() == false)
                            return;

                        AddDataCollection(d, GetNewName());
                    };
                    propertyGridWindow.SetObjectView(query);
                }, obj => obj != null));


            var visitData = new BindingAction("浏览方式");

            IEnumerable<Command> visitCommands = PluginProvider.GetPluginCollection(typeof (IDataViewer)).Select(
                d =>
                {
                    var comm = new Command(d.Name);
                    comm.Execute = d2 =>
                    {
                        var data = d2 as DataCollection;

                        if (data.Count == 0)
                        {
                            MessageBox.Show("不存在任何数据", "提示信息");
                            return;
                        }


                        ControlExtended.UIInvoke(() =>
                        {
                            var view = PluginProvider.GetObjectInstance<IDataViewer>(d.Name);

                            object r = view.SetCurrentView(data.ComputeData);

                            if (ControlExtended.DockableManager != null)
                            {
                                ControlExtended.DockableManager.AddDockAbleContent(
                                    FrmState.Float, r, data.Name + " " + d.Name);
                            }
                        });
                    };
                    return comm;
                });
            visitData.Execute = obj => visitCommands.FirstOrDefault(d => d.Text == "可编辑列表").Execute(obj);
            foreach (Command visitCommand in visitCommands)
            {
                visitData.ChildActions.Add(visitCommand);
            }

            dataaction.ChildActions.Add(new Command(
                "数据清洗", obj =>
                {
                    DataCollection collection = GetCollection(obj);
                    if (collection == null) return;

                    var plugin = processManager.GetOneInstance("SmartETLTool", true, true,true) as SmartETLTool;
                    plugin.Init();
                    dynamic generator = PluginProvider.GetObjectByType<IColumnProcess>("TableGE");
                    generator.TableSelector.SelectItem = collection;
                    plugin.CurrentETLTools.Add(generator);
                    plugin.RefreshSamples(true);
                    ControlExtended.DockableManager.ActiveModelContent(plugin);
                }, obj => true, "new"));
            dataaction.ChildActions.Add(new Command(
                "拷贝", obj =>
                {
                    DataCollection collection = GetCollection(obj);
                    if (collection == null) return;
                    DataCollection n = collection.Clone(true);
                    n.Name = GetNewName(collection.Name);
                    DataCollections.Add(n);
                }, obj => true, "page_new"));
            var saveData = new Command("另存为", d =>
            {
                DataCollection collection = GetCollection(d);
                if (collection == null)
                    return;
                var ofd = new SaveFileDialog {Filter = FileConnector.GetDataFilter(), DefaultExt = "*"};

                ofd.FileName = collection.Name + ".xlsx";
                if (ofd.ShowDialog() == true)
                {
                    string filename = ofd.FileName;
                    SaveFile(collection.Name, filename);
                }
            }, obj => true, "save");

            dataaction.ChildActions.Add(saveData);
            dataaction.ChildActions.Add(visitData);
            dataaction.ChildActions.Add(new Command(
                "新建",
                obj =>
                    DataCollections.Add(new DataCollection(new List<IFreeDocument>())
                    {
                        Name = GetNewName("新建数据集")
                    }), obj => true, "box"));
            dataaction.ChildActions.Add(new Command(
                "配置", obj =>
                {
                    DataCollection collection = GetCollection(obj);
                    if (collection != null) propertyGridWindow.SetObjectView(collection);
                }, obj => true, "settings"));
            dataaction.ChildActions.Add(new Command(
                "删除", obj =>
                {
                    DataCollection collection = GetCollection(obj);
                    if (collection != null) DataCollections.Remove(collection);
                }, obj => true, "delete"));

            var convert = new BindingAction("转换表类型");
            dataaction.ChildActions.Add(convert);
            convert.ChildActions.Add(new Command("转为非虚拟数据集", obj =>
            {
                DataCollection coll = GetCollection(obj);
                if (coll.Count > 500000)
                {
                    if (
                        MessageBox.Show("本集合数据量较大，转换可能会占用较高的内存和导致程序崩溃，确定继续吗?", "提示信息", MessageBoxButton.YesNoCancel) !=
                        MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                var docuts = new List<IFreeDocument>();
                var task=TemporaryTask.AddTempTask("转为非虚拟数据集", coll.ComputeData, d =>
                {
                    if (d != null)
                        docuts.Add(d as IFreeDocument);
                }, result =>
                {
                    var collection = new DataCollection(docuts) {Name = coll.Name + '1'};
                    AddDataCollection(collection);
                    this.DataCollections.Remove(coll);
                });
                processManager.CurrentProcessTasks.Add(task);
            }));
            var insertdb = new BindingAction("保存到数据库");
            insertdb.SetChildActionSource(() =>
            {
                return _dbConnections.Select(dataBaseConnector => new Command(dataBaseConnector.Name, obj =>
                {
                    var data = obj as DataCollection;
                    processManager.CurrentProcessTasks.Add(TemporaryTask.AddTempTask(data.Name + "插入到数据库", dataBaseConnector.InserDataCollection(data), result => dataBaseConnector.RefreshTableNames(), count: data.Count/1000));
                })).Cast<ICommand>().ToList();
            });


            dataaction.ChildActions.Add(insertdb);
            var otherDataAction = new BindingAction();
            otherDataAction.ChildActions.Add(new Command("清空数据", obj => CleanData(), obj => DataCollections.Count > 0));


            commands.Add(dbaction);
            commands.Add(tableAction);
            commands.Add(dataaction);
            commands.Add(otherDataAction);
            var dblistAction = new BindingAction("数据库管理");

            var addnew = new BindingAction("增加新连接");
            dblistAction.ChildActions.Add(addnew);
            foreach (XFrmWorkAttribute item in PluginProvider.GetPluginCollection(typeof (IDataBaseConnector)))
            {
                addnew.ChildActions.Add(new Command(item.Name)
                {
                    Execute = obj =>
                    {
                        var con = PluginProvider.GetObjectInstance(item.MyType) as DBConnectorBase;
                        con.Name = item.Name;

                        _dbConnections.Add(con);
                    }
                });
            }
            commands.Add(dblistAction);


            dockableManager = MainFrmUI as IDockableManager;
            if (processManager?.CurrentProject != null)

            {
                LoadDataConnections();
            }
            processManager.OnCurrentProjectChanged += (s, e) => LoadDataConnections();

            ConfigUI = PropertyGridFactory.GetInstance(_dbConnections.FirstOrDefault());


            propertyGridWindow = MainFrmUI.PluginDictionary["属性配置器"] as XFrmWorkPropertyGrid;
            var changed = DataCollections as INotifyCollectionChanged;

            changed.CollectionChanged += (s, e) => OnDataSourceChanged(new EventArgs());

            return true;
        }


        public override void SaveConfigFile()
        {
        }

        private void LoadDataConnections()
        {
            _dbConnections = processManager?.CurrentProject?.DBConnections;
            InformPropertyChanged("CurrentConnectors");
            foreach (var  dataBaseConnector in processManager.CurrentProject.DBConnections.Where(d=>d.AutoConnect==true
))
            {
                var db = (DBConnectorBase) dataBaseConnector;
                if (db.ConnectDB() == false)
                {
                    XLogSys.Print.Error(db.Name + "的数据库连接服务失效");
                }
                else
                {
                    XLogSys.Print.Info(db.Name + "的数据连接服务成功！");

                    db.RefreshTableNames();
                }
            }
        }

        #endregion

        #region Implemented Interfaces

        #region IDataManager

        public DataCollection AddDataCollection(
            IEnumerable<IFreeDocument> source, string collectionName = null, bool isCover = false)
        {
            if (collectionName == null)
            {
                collectionName = "数据集" + DateTime.Now.ToShortTimeString();
            }
 
            DataCollection collection = DataCollections.FirstOrDefault(d => d.Name == collectionName);

            if (collection != null)
            {
                if (!isCover)
                {
                    foreach (IFreeDocument computeable in source)
                    {
                        collection.ComputeData.Add(computeable);
                    }
                    collection.OnPropertyChanged("Count");
                }
                else
                {
                    XLogSys.Print.Warn(collectionName + "数据源已经存在，不进行覆盖，没有保存");
                }
                return collection;
            }
            var data = new DataCollection(source.ToList()) {Name = collectionName};

            DataCollections.Add(data);
            return data;
        }

        public void AddDataCollection(DataCollection collection)
        {
            DataCollections.Add(collection);
        }

        public IList<IFreeDocument> Get(string name)
        {
            if (DataCollections.Count == 0)
            {
                return new List<IFreeDocument>();
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                return DataCollections.First().ComputeData.ToList();
            }
            DataCollection p = DataCollections.FirstOrDefault(rc => rc.Name == name);
            return p?.ComputeData;
        }

        public DataCollection GetCollection(string name)
        {
            if (DataCollections.Count == 0)
            {
                return null;
            }
            return string.IsNullOrWhiteSpace(name) ? DataCollections.FirstOrDefault() : DataCollections.FirstOrDefault(rc => rc.Name == name);
        }


        public void SaveFile(string dataCollectionName, string path = null, string format = null)
        {
            DataCollection data = DataCollections.FirstOrDefault(d => d.Name == dataCollectionName);
            if (data == null)
                return;
            SaveFile(data, path, format);
        }

        public void SaveFile(DataCollection dataCollection, string path = null, string format = null)
        {
            IFileConnector exporter = null;
            if (format != null)
            {
                exporter = PluginProvider.GetObjectInstance<IFileConnector>(format);
            }
            else
            {
                exporter = FileConnector.SmartGetExport(path);
            }

            if (exporter == null)
            {
                return;
            }
            IList<IFreeDocument> data = dataCollection.ComputeData;


            exporter.FileName = path;
            processManager.CurrentProcessTasks.Add(TemporaryTask.AddTempTask(dataCollection + "导出数据任务", exporter.WriteData(data), null, result =>
            {
                if (MainDescription.IsUIForm && string.IsNullOrEmpty(exporter.FileName) == false)
                {
                    if (MessageBox.Show("文件导出成功，是否要打开查看?", "提示信息", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(exporter.FileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("打开文件失败：" + ex.Message);
                        }
                    }
                }
            }, data.Count,notifyInterval:1000));
        }

        #endregion

        #endregion

        #region Methods

        private void CleanData()
        {
            if (MessageBox.Show("确定删除内存数据么？", "警告信息", MessageBoxButton.OKCancel, MessageBoxImage.Question) ==
                MessageBoxResult.Cancel)
            {
                return;
            }
            DataCollections.Clear();
            XLogSys.Print.Info("当前内存数据已经被清除");
        }


        private void DropTable(IDataBaseConnector connector, string dataName)
        {
            if (
                MessageBox.Show(
                    "确定对数据表" + dataName + "执行删除操作吗？", "警告信息", MessageBoxButton.YesNo) ==
                MessageBoxResult.Yes)
            {
                connector.DropTable(dataName);
                connector.RefreshTableNames();
            }
        }


        private void OnDataSourceChanged(EventArgs e)
        {
            EventHandler handler = DataSourceChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void RefreshConnect(IDataBaseConnector connector)
        {
            if (!connector.ConnectDB())
            {
                MessageBox.Show(connector.Name + "强制刷新连接失败");
                return;
            }
            connector.RefreshTableNames();
        }

        #endregion

        //AddFreeDocument
    }
}