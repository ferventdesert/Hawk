using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        public List<ICommand> commands;
        public Action<List<FreeDocument>> GetQueryFunc;

        public QueryEntity()
        {
            commands = new List<ICommand>();
            commands.Add(new Command(GlobalHelper.Get("key_213"), async obj =>
            {
                List<FreeDocument> datas = null;
                try
                {
                    ControlExtended.SetBusy(ProgressBarState.Indeterminate);
                    int count;
                    datas = await Task.Run(() => Connector.QueryEntities(SQL,
                        out count, TableInfo == null ? null : TableInfo.Name));
                    ControlExtended.SetBusy(ProgressBarState.NoProgress);
                    XLogSys.Print.Info(GlobalHelper.Get("key_325"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(GlobalHelper.Get("key_214") + ex.Message);
                    XLogSys.Print.Error(ex);
                    return;
                }
                finally
                {
                    ControlExtended.SetBusy(ProgressBarState.NoProgress);
                }
                GetQueryFunc(datas);
            }, obj => string.IsNullOrEmpty(SQL) == false, "page_search"));
        }

        [LocalizedDisplayName("key_215")]
        [PropertyOrder(1)]
        public TableInfo TableInfo { get; set; }

        [LocalizedDisplayName("key_216")]
        [PropertyOrder(0)]
        public IDataBaseConnector Connector { get; set; }

        [LocalizedDisplayName("key_217")]
        [LocalizedDescription("key_218")]
        [StringEditor("SQL")]
        [PropertyOrder(2)]
        [PropertyEditor("CodeEditor")]
        public string SQL { get; set; }

        [PropertyOrder(3)]
        [LocalizedDisplayName("key_34")]
        public ReadOnlyCollection<ICommand> Commands => new ReadOnlyCollection<ICommand>(commands);
    }

    [XFrmWork("DataManager", "DataManager_desc", "")]
    public class DataManager : AbstractPlugIn, IDataManager, IView
    {
        #region Constants and Fields

        private IDockableManager dockableManager;

        #endregion

        #region Events

        public ICollection<IDataBaseConnector> CurrentConnectors => _dbConnections;

        public event EventHandler DataSourceChanged;

        private ListBox dataListBox;
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
            get { return DataCollections?.Select(i => i.Name); }
        }


        public FrmState FrmState => FrmState.Mini;

        public object UserControl => null;

        #endregion

        #region Public Methods

        private IProcessManager processManager;

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
                () => AddDataCollection(exporter.ReadFile(), Path.GetFileNameWithoutExtension(fileName)),
                LogType.Important);
            return GetSelectedCollection(fileName);
        }

        public DataCollection ReadCollection(IDataBaseConnector connector, string tableName, bool isVirtual)
        {
            if (isVirtual)
            {
                IItemsProvider<IFreeDocument> vir = null;
                var tableInfo = connector.RefreshTableNames().FirstOrDefault(d => d.Name == tableName);

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
                var count = 1000;
                if (connector.TypeName == GlobalHelper.Get("key_221"))
                    count = 100;
                var col = new VirtualDataCollection(vir, count)
                {
                    Name = tableInfo.Name
                };
                AddDataCollection(col);
                return col;
            }
            else
            {
                var datas = GetDataFromDB(connector, tableName, true);
                var col = AddDataCollection(datas.Result);
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

            var task = TemporaryTask<FreeDocument>.AddTempTaskSimple(dataName + GlobalHelper.Get("key_222"),
                db.GetEntities(dataName, mount), dataAll.Add, null, table?.Size ?? -1,
                notifyInterval: 1000);
            processManager.CurrentProcessTasks.Add(task);
            await Task.Run(
                () => task.Wait());
            return dataAll;
        }

        private IEnumerable<DataCollection> GetSelectedCollection(object data)
        {
            if (data == null)
            {
                 foreach(var col in dataListBox.SelectedItems.IListConvert<DataCollection>())
                {
                    yield return col as DataCollection;
                }
                 yield break;
            }


            if (data is DataCollection)
            {
                 yield return data as DataCollection;
            }
        }


        private string GetNewName(string name = null)
        {
            if (name == null)
                name = GlobalHelper.Get("key_239");
            if (DataCollections.Any(d => d.Name == name))
            {
                var item =
                    name.Last();
                if (char.IsDigit(item))
                {
                    var p = int.Parse(item.ToString());
                    name = name.Substring(name.Length - 2) + (p + 1);
                }
                else
                {
                    name += '1';
                }
            }
            return name;
        }

        public DataManager()
        {
            _dbConnections = new ObservableCollection<IDataBaseConnector>();
        }
        public override bool Init()
        {
            if (MainDescription.IsUIForm)
            {
                DataCollections = new SafeObservable<DataCollection>();
                dockableManager = MainFrmUI as IDockableManager;
                var views = "223:Mini 794:Middle";
                foreach (var item in views.Split(' '))
                {
                    var item2 = item.Split(':');
                    var name = item2[0];
                    var control = FrmState.Mini;
                    Enum.TryParse(item2[1], out control);
                    var itemName = "key_" + name;
                    itemName = GlobalHelper.Get(itemName);
                    var view = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get(itemName));
                    var userControl = view as UserControl;
                    if (userControl != null)
                    {
                        if(name=="223")
                        {
                            dynamic dcontrol = userControl;
                            dataListBox=    dcontrol.dataListBox as ListBox;
                        }
                        userControl.DataContext = MainFrmUI;
                        dockableManager.AddDockAbleContent(control, view, itemName);
                    }
                }
            
            }

            else
            {
                DataCollections = new ObservableCollection<DataCollection>();
              
            }

            processManager = MainFrmUI.PluginDictionary["DataProcessManager"] as IProcessManager;


            commands = new List<ICommand>();
            var dbaction = new BindingAction();
            dbaction.ChildActions.Add(new Command(GlobalHelper.Get("key_224"),
                obj =>
                {
                    var w = PropertyGridFactory.GetPropertyWindow(obj);
                    w.ShowDialog();
                },
                obj => obj != null, "edit"));
            dbaction.ChildActions.Add(
                new Command(GlobalHelper.Get("key_142"), obj => RefreshConnect(obj as IDataBaseConnector),
                    obj => obj != null, "refresh"));
            dbaction.ChildActions.Add(
                new Command(GlobalHelper.Get("key_213"), obj =>
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
                    PropertyGridFactory.GetPropertyWindow(query).ShowDialog();
                }, obj => obj != null, "magnify"));
            dbaction.ChildActions.Add(
                new Command(GlobalHelper.Get("key_225"), obj =>
                {
                    if (
                        MessageBox.Show(GlobalHelper.Get("key_226"), GlobalHelper.Get("key_99"),
                            MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                    {
                        var con = obj as DBConnectorBase;
                        _dbConnections.Remove(con);
                    }
                }, obj => obj != null, "delete"));
            var dataaction = new BindingAction();


            var tableAction = new BindingAction();
            tableAction.ChildActions.Add(new Command(
                GlobalHelper.Get("view"),
                async obj =>
                {
                    var items = obj as TableInfo;
                    List<IFreeDocument> dataAll = null;
                    try
                    {
                        dataAll = await
                            GetDataFromDB(items.Connector, items.Name, true,
                                items.Connector is FileManager ? -1 : 200);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(GlobalHelper.Get("key_228") + ex.Message);
                        return;
                    }


                    if (dataAll == null || dataAll.Count == 0)
                    {
                        XLogSys.Print.Warn(GlobalHelper.Get("key_229"));
                        return;
                    }
                    if (items.Connector is FileManager)
                    {
                        var file = (items.Connector as FileManager).LastFileName;
                        var name = Path.GetFileNameWithoutExtension(file);
                        AddDataCollection(dataAll, name);
                        return;
                    }
                    var excel = PluginProvider.GetObjectInstance<IDataViewer>(GlobalHelper.Get("key_230"));
                    if (excel == null)
                        return;
                    var view = excel.SetCurrentView(dataAll.Select(d => d).ToList());

                    if (ControlExtended.DockableManager != null)
                    {
                        ControlExtended.DockableManager.AddDockAbleContent(
                            FrmState.Custom, view, items.Name);
                    }
                },
                obj => obj != null));
            tableAction.ChildActions.Add(new Command(
                GlobalHelper.Get("key_231"),
                async obj =>
                {
                    var items = obj as TableInfo;
                    var datas = await GetDataFromDB(items.Connector, items.Name, true);
                    if (datas == null)
                        return;
                    AddDataCollection(datas, items.Name);
                },
                obj => obj != null, "add"));
            tableAction.ChildActions.Add(new Command(
                GlobalHelper.Get("key_232"),
                obj =>
                {
                    var con = obj as TableInfo;
                    ReadCollection(con.Connector, con.Name, true);
                },
                obj => obj != null, "layer_add"));
            tableAction.ChildActions.Add(new Command(
                GlobalHelper.Get("key_233"),
                obj =>
                {
                    var items = obj as TableInfo;

                    DropTable(items.Connector, items.Name);
                },
                obj => obj != null, "delete"));
            tableAction.ChildActions.Add(new Command(
                GlobalHelper.Get("key_234"),
                obj =>
                {
                    var w = PropertyGridFactory.GetPropertyWindow(obj);
                    w.ShowDialog();
                },
                obj => obj != null, "edit"));
            tableAction.ChildActions.Add(new Command(
                GlobalHelper.Get("key_213"),
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
                    PropertyGridFactory.GetPropertyWindow(query).ShowDialog();
                }, obj => obj != null, "magnify"));
           

            var visitData = new BindingAction(GlobalHelper.Get("key_235"));

            var visitCommands = PluginProvider.GetPluginCollection(typeof (IDataViewer)).Select(
                d =>
                {
                    var comm = new Command(d.Name);
                    comm.Execute = d2 =>
                    {
                        var data = d2 as DataCollection;

                        if (data.Count == 0)
                        {
                            MessageBox.Show(GlobalHelper.Get("key_236"), GlobalHelper.Get("key_99"));
                            return;
                        }


                        ControlExtended.UIInvoke(() =>
                        {
                            var view = PluginProvider.GetObjectInstance<IDataViewer>(d.Name);

                            var r = view.SetCurrentView(data.ComputeData);

                            if (ControlExtended.DockableManager != null)
                            {
                                ControlExtended.DockableManager.AddDockAbleContent(
                                    FrmState.Float, r, data.Name + " " + d.Name);
                            }
                        });
                    };
                    return comm;
                });
            visitData.Execute =
                obj => visitCommands.FirstOrDefault(d => d.Text == GlobalHelper.Get("key_230")).Execute(obj);
            foreach (var visitCommand in visitCommands)
            {
                visitData.ChildActions.Add(visitCommand);
            }

            dataaction.ChildActions.Add(new Command(
                GlobalHelper.Get("smartetl_name"), obj =>
                {
                    var collection = GetSelectedCollection(obj).FirstOrDefault();
                    if (collection == null) return;

                    var plugin = processManager.GetOneInstance("SmartETLTool", true, true, true) as SmartETLTool;

                    dynamic generator = PluginProvider.GetObjectByType<IColumnProcess>("TableGE");
                    generator.Father = plugin;

                    generator.TableSelector.SelectItem = collection.Name;
                    plugin.CurrentETLTools.Add(generator);
                    plugin.ETLMount++;
                    plugin.Init();

                    //plugin.RefreshSamples(true);
                    ControlExtended.DockableManager.ActiveModelContent(plugin);
                }, obj => true, "new"));

            var saveData = new Command(GlobalHelper.Get("key_237"), d =>
            {
                var collection = GetSelectedCollection(d).FirstOrDefault();
                if (collection == null)
                    return;
                var ofd = new SaveFileDialog {Filter = FileConnector.GetDataFilter(), DefaultExt = "*"};

                ofd.FileName = collection.Name + ".xlsx";
                if (ofd.ShowDialog() == true)
                {
                    var filename = ofd.FileName;
                    SaveFile(collection.Name, filename);
                }
            }, obj => true, "save");

            dataaction.ChildActions.Add(saveData);
            dataaction.ChildActions.Add(visitData);
            dataaction.ChildActions.Add(new Command(
                GlobalHelper.Get("key_238"),
                obj =>
                {
                    if (obj != null)
                    {
                        foreach(var collection in GetSelectedCollection(obj))
                        {
                            if (collection == null) return;
                            var n = collection.Clone(true);
                            n.Name = GetNewName(collection.Name);
                            DataCollections.Add(n);
                        }
                    }
                    else
                    {
                        DataCollections.Add(new DataCollection(new List<IFreeDocument>())
                        {
                            Name = GetNewName(GlobalHelper.Get("key_239"))
                        });
                    }
                    ;
                }, obj => true, "add"));
            dataaction.ChildActions.Add(new Command(
                GlobalHelper.Get("key_240"), obj =>
                {
                    var collection = GetSelectedCollection(obj);
                    if (collection != null) PropertyGridFactory.GetPropertyWindow(collection).ShowDialog();
                }, obj => true, "settings"));
            dataaction.ChildActions.Add(new Command(
                GlobalHelper.Get("key_169"), obj =>
                {
                    foreach(var  collection  in GetSelectedCollection(obj))
                    {

                    if (collection != null) DataCollections.Remove(collection);
                    }
                }, obj => true, "delete"));

            var convert = new BindingAction(GlobalHelper.Get("key_241"));
            dataaction.ChildActions.Add(convert);
            convert.ChildActions.Add(new Command(GlobalHelper.Get("key_242"), obj =>
            {
                var coll = GetSelectedCollection(obj).FirstOrDefault();
                if (coll == null)
                    return;
                if (coll.Count > 500000)
                {
                    if (
                        MessageBox.Show(GlobalHelper.Get("key_243"), GlobalHelper.Get("key_99"),
                            MessageBoxButton.YesNoCancel) !=
                        MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                var docuts = new List<IFreeDocument>();
                var task = TemporaryTask<FreeDocument>.AddTempTaskSimple(GlobalHelper.Get("key_242"), coll.ComputeData, d =>
                {
                    if (d != null)
                        docuts.Add(d);
                }, result =>
                {
                    var collection = new DataCollection(docuts) {Name = coll.Name + '1'};
                    AddDataCollection(collection);
                    DataCollections.Remove(coll);
                });
                processManager.CurrentProcessTasks.Add(task);
            }));
            var insertdb = new BindingAction(GlobalHelper.Get("key_244"));
            insertdb.SetChildActionSource(() =>
            {
                return _dbConnections.Select(dataBaseConnector => new Command(dataBaseConnector.Name, obj =>
                {
                    var data = obj as DataCollection;
                    processManager.CurrentProcessTasks.Add(
                        TemporaryTask<FreeDocument>.AddTempTaskSimple(data.Name + GlobalHelper.Get("key_245"),
                            dataBaseConnector.InserDataCollection(data), result => dataBaseConnector.RefreshTableNames(),
                            count: data.Count/1000));
                }, icon: "database")).Cast<ICommand>().ToList();
            });


            dataaction.ChildActions.Add(insertdb);
            var otherDataAction = new BindingAction();
            otherDataAction.ChildActions.Add(new Command(GlobalHelper.Get("key_132"), obj => CleanData(),
                obj => DataCollections.Count > 0,
                "clear"));


            commands.Add(dbaction);
            commands.Add(tableAction);
            commands.Add(dataaction);
            commands.Add(otherDataAction);
            var dblistAction = new BindingAction(GlobalHelper.Get("key_246"));

            var addnew = new BindingAction(GlobalHelper.Get("key_247")) {Icon = "add"};
            dblistAction.ChildActions.Add(addnew);
            foreach (var item in PluginProvider.GetPluginCollection(typeof (IDataBaseConnector)))
            {
                addnew.ChildActions.Add(new Command(item.Name)
                {
                    Execute = obj =>
                    {
                        var con = PluginProvider.GetObjectInstance(item.MyType) as DBConnectorBase;
                        con.Name = item.Name;

                        _dbConnections.Add(con);
                    },
                    Icon = "connect"
                });
            }
            commands.Add(dblistAction);


            dockableManager = MainFrmUI as IDockableManager;
            if (processManager?.CurrentProject != null)

            {
                LoadDataConnections(processManager.CurrentProject.DBConnections);
            }
          
            if (MainDescription.IsUIForm)
            {
                ConfigUI = PropertyGridFactory.GetInstance(_dbConnections.FirstOrDefault());
            }

            var changed = DataCollections as INotifyCollectionChanged;

            changed.CollectionChanged += (s, e) => OnDataSourceChanged(new EventArgs());

            return true;
        }


        public override void SaveConfigFile()
        {
        }

        public void LoadDataConnections(ICollection<IDataBaseConnector> connectors )
        {
            CurrentConnectors.Clear();
            _dbConnections.AddRange(connectors);
            InformPropertyChanged("CurrentConnectors");
            foreach (var  dataBaseConnector in CurrentConnectors.Where(d => d.AutoConnect
                ))
            {
                var db = (DBConnectorBase) dataBaseConnector;
                if (db.ConnectDB() == false)
                {
                    XLogSys.Print.Error(db.Name + GlobalHelper.Get("key_248"));
                }
                else
                {
                    XLogSys.Print.Info(db.Name + GlobalHelper.Get("key_249"));

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
                collectionName = GlobalHelper.Get("key_250") + DateTime.Now.ToShortTimeString();
            }

            var collection = DataCollections.FirstOrDefault(d => d.Name == collectionName);

            if (collection != null)
            {
                if (!isCover)
                {
                    foreach (var computeable in source)
                    {
                        collection.ComputeData.Add(computeable);
                    }
                    collection.OnPropertyChanged("Count");
                }
                else
                {
                    XLogSys.Print.Warn(collectionName + GlobalHelper.Get("key_251"));
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
            var p = DataCollections.FirstOrDefault(rc => rc.Name == name);
            return p?.ComputeData;
        }

        public DataCollection GetSelectedCollection(string name)
        {
            if (DataCollections.Count == 0)
            {
                return null;
            }
            return string.IsNullOrWhiteSpace(name)
                ? DataCollections.FirstOrDefault()
                : DataCollections.FirstOrDefault(rc => rc.Name == name);
        }


        public void SaveFile(string dataCollectionName, string path = null, string format = null)
        {
            var data = DataCollections.FirstOrDefault(d => d.Name == dataCollectionName);
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
            var data = dataCollection.ComputeData;


            exporter.FileName = path;
            processManager.CurrentProcessTasks.Add(
                TemporaryTask<FreeDocument>.AddTempTask(dataCollection + GlobalHelper.Get("key_252"),
                    exporter.WriteData(data), null, result =>
                    {
                        if (MainDescription.IsUIForm && string.IsNullOrEmpty(exporter.FileName) == false)
                        {
                            if (
                                MessageBox.Show(GlobalHelper.Get("key_253"), GlobalHelper.Get("key_99"),
                                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                            {
                                try
                                {
                                    System.Diagnostics.Process.Start(exporter.FileName);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(GlobalHelper.Get("key_254") + ex.Message);
                                }
                            }
                        }
                    }, data.Count, notifyInterval: 1000));
        }

        #endregion

        #endregion

        #region Methods

        private void CleanData()
        {
            if (
                MessageBox.Show(GlobalHelper.Get("key_255"), GlobalHelper.Get("key_151"), MessageBoxButton.OKCancel,
                    MessageBoxImage.Question) ==
                MessageBoxResult.Cancel)
            {
                return;
            }
            DataCollections.Clear();
            XLogSys.Print.Info(GlobalHelper.Get("key_256"));
        }


        private void DropTable(IDataBaseConnector connector, string dataName)
        {
            if (
                MessageBox.Show(
                    GlobalHelper.Get("key_257") + dataName + GlobalHelper.Get("key_258"), GlobalHelper.Get("key_151"),
                    MessageBoxButton.YesNo) ==
                MessageBoxResult.Yes)
            {
                connector.DropTable(dataName);
                connector.RefreshTableNames();
            }
        }


        private void OnDataSourceChanged(EventArgs e)
        {
            var handler = DataSourceChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void RefreshConnect(IDataBaseConnector connector)
        {
            if (!connector.ConnectDB())
            {
                MessageBox.Show(connector.Name + GlobalHelper.Get("key_259"));
                return;
            }
            connector.RefreshTableNames();
        }

        #endregion
    }
}