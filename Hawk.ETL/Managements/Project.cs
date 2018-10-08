using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;
using YamlDotNet;
using YamlDotNet.RepresentationModel;
namespace Hawk.ETL.Managements
{
    public class ProjectItem : PropertyChangeNotifier, IDictionarySerializable
    {
        [Browsable(false)]
        public string SavePath { get; set; }

        [PropertyOrder(0)]
        [LocalizedCategory("key_211")]
        [LocalizedDisplayName("key_18")]
        public string Name { get; set; }

        [PropertyOrder(1)]
        [LocalizedCategory("key_211")]
        [LocalizedDisplayName("key_16")]
        [PropertyEditor("CodeEditor")]
        public string Description { get; set; }

        [Browsable(false)]
        [LocalizedDisplayName("key_330")]
        public int Version { get; set; }


        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("Name", Name);
            dict.Add("Description", Description);
            dict.Add("Version", Version);
            dict.Add("SavePath", SavePath);
            return dict;
        }

        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Name = docu.Set("Name", Name);
            Description = docu.Set("Description", Description);
            Version = docu.Set("Version", Version);
            SavePath = docu.Set("SavePath", SavePath);
        }
    }

    public class ParameterItem
    {
        private  static  Random random=new Random();
        public ParameterItem()
        {

            Name = "config_" + random.Next(0, 100);
        }

        [PropertyOrder(0)]
     
        [LocalizedDisplayName("key_18")]
        public string Name { get; set; }

        [Browsable(false)]
        public Dictionary<string, string> GetParameters()
        {
            var dict= 
                new Dictionary<string, string>();
            if (string.IsNullOrEmpty(ParameterString))
                return dict;
           var stream = new StringReader(ParameterString);
            var yaml = new YamlStream();

            yaml.Load(stream);
            var mapping =
                (YamlMappingNode)yaml.Documents[0].RootNode;

            foreach (var entry in mapping.Children)
            {
                if ((entry.Key.NodeType == YamlNodeType.Scalar) && (entry.Value.NodeType == YamlNodeType.Scalar))
                {
                    var key = ((YamlScalarNode) entry.Key).Value;

                    var value = ((YamlScalarNode) entry.Value).Value;
                    dict.Add(key, value);
                }
            }
        
            return dict;

            
        }

        public override string ToString()
        {
            return Name;
        }
        [LocalizedDisplayName("key_21")]
        [PropertyEditor("CodeEditor")]
        public string ParameterString
        {
            get;set;
        }
    }
    /// <summary>
    ///     项目信息
    /// </summary>
    public class Project : ProjectItem
    {
        private readonly IProcessManager sysProcessManager;

        public Project()
        {
            Tasks = new ObservableCollection<ProcessTask>();
            DBConnections = new ObservableCollection<IDataBaseConnector>();
            Parameters =new  ObservableCollection<ParameterItem>();
                sysProcessManager = MainDescription.MainFrm.PluginDictionary["DataProcessManager"] as IProcessManager;
            ConfigSelector=new ExtendSelector<string>();
            Parameters.CollectionChanged += (s, e) =>
            {
                ConfigSelector.OnPropertyChanged("Collection");
            };
            ConfigSelector.GetItems = () => { return Parameters.Select(d => d.Name).ToList(); };
        }

        [Browsable(false)]
        public List<DataCollection> DataCollections { get; set; }

        [LocalizedDisplayName("key_332")]
        [Browsable(false)]
        public ObservableCollection<IDataBaseConnector> DBConnections { get; set; }

        private List<FreeDocument> SavedRunningTasks;
         
        [LocalizedDisplayName("using_param_name")]
        [PropertyOrder(3)]
        public ExtendSelector<string> ConfigSelector { get; set; }

        /// <summary>
        ///     在工程中保存的所有任务
        /// </summary>
        [Browsable(false)]
        public ObservableCollection<ProcessTask> Tasks { get; set; }
        
        [LocalizedDisplayName("param_group")]
        [PropertyOrder(4)]
        public ObservableCollection<ParameterItem> Parameters { get; private set; }

        public void Save(IEnumerable<DataCollection>collections = null)
        {
            var connector = new FileConnectorXML();

            if (SavePath != null && File.Exists(SavePath))
            {
                connector.FileName = SavePath;
            }
            else
            {
                var result = connector.CheckFilePath(FileOperate.Save);
                if (result == false) return;
                SavePath = connector.FileName;
            }
            var ext = Path.GetExtension(SavePath);
            if (ext != null && ext.Contains("hproj"))
                connector.IsZip = true;
            var dict = DictSerialize();
                if (collections != null)
                dict["DataCollections"] = new FreeDocument
                {
                    Children = collections.Where(d => d.Count < 100000).Select(d => d.DictSerialize()).ToList()
                };
            connector.WriteAll(
                new List<IFreeDocument> {dict}
            );
        }

        public static Project Load(string path = null)
        {
            var connector = new FileConnectorXML();

            connector.FileName = path;
            if (connector.FileName == null)
            {
                var result = connector.CheckFilePath(FileOperate.Read);
                if (result == false)
                    return null;
            }
            else
            {
                if (!File.Exists(connector.FileName))
                {
                    XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_334"), connector.FileName));
                    return null;
                }
            }
            var ext = Path.GetExtension(connector.FileName);
            if (ext != null && ext.Contains("hproj"))
                connector.IsZip = true;
            var projfile = connector.ReadFile().FirstOrDefault();

            if (projfile == null)
                return null;
            var proj = new Project();
            projfile.DictCopyTo(proj);

            object collectionObj = null;
            if (projfile.TryGetValue("DataCollections", out collectionObj))
            {
                var collectionDocs = (collectionObj as FreeDocument)?.Children;
                proj.DataCollections = collectionDocs?.Select(d =>
                {
                    var doc = new DataCollection();
                    doc.DictDeserialize(d);
                    return doc;
                }).ToList();
            }
            proj.SavePath = connector.FileName;

            return proj;
        }

        [Browsable(false)]
        public Dictionary<string, string> Parameter { get; private set; }

        public void Build()
        {
            ParameterItem param;

            if (ConfigSelector?.SelectItem == null)
                param = Parameters.FirstOrDefault();
            else
                 param = this.Parameters.FirstOrDefault(d => d.Name == ConfigSelector.SelectItem);
            if (param == null)
            {
                Parameter = new Dictionary<string, string>();
                return;
            }

            ControlExtended.SafeInvoke(() => Parameter = param.GetParameters(),LogType.Info,  GlobalHelper.Get("parse_yaml_config"));
        }
        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize();

            dict.Children = Tasks.Select(d => d.DictSerialize()).ToList();
            var connectors = new FreeDocument
            {
                Children = DBConnections.Select(d => (d as IDictionarySerializable).DictSerialize()).ToList()
            };
            dict.Add("DBConnections", connectors);


            var param = new FreeDocument
            {
                Children = Parameters.Select(d => d.UnsafeDictSerialize()).ToList()

            };

            if (ConfigSelector.SelectItem != null)
            {
                dict.Add("ParameterName", ConfigSelector.SelectItem);
            }


            dict.Add("Parameters", param);



            if (sysProcessManager != null)
            {
                var runningTasks = new FreeDocument
                {
                    Children =
                        sysProcessManager.CurrentProcessTasks
                            .Where(d => d.IsCanceled == false && d.Publisher is SmartETLTool)
                            .OfType<IDictionarySerializable>().Select(d => d.DictSerialize())
                            .ToList()
                };
                dict.Add("RunningTasks", runningTasks);
            }
            return dict;
        }

        public void LoadRunningTasks()
        {
           if(SavedRunningTasks==null)
                return;
            foreach (var items in SavedRunningTasks.GroupBy(d => d["Publisher"]))
            {

                var publisherName = items.Key.ToString();
                if (publisherName == null)
                    continue;


                var publisher =
                    sysProcessManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == publisherName);
                if (publisher == null)
                {
                    var task = Tasks.FirstOrDefault(d => d.Name == publisherName);
                    if (task == null)
                    {
                        XLogSys.Print.Info("TODO");
                        continue;

                    }
                    task.Load(true);
                    publisher =
                        sysProcessManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == publisherName);

                }
                if (publisher == null)
                {
                    XLogSys.Print.Info("TODO");
                    continue;
                }
                var tool = publisher as SmartETLTool;
                if (tool == null)
                {
                    XLogSys.Print.Info("TODO");
                    continue;
                }
                tool.InitProcess(true);
                //tool.RefreshSamples(false);
                var runningTasks = items.Select(d =>
                {
                    var rtask = new TemporaryTask<IFreeDocument>();
                    rtask.DictDeserialize(d);
                    return rtask;
                }).ToList();
                tool.ExecuteDatas(runningTasks);




                //sysProcessManager.

                //RunningTasks.Add(conn);
            }
        }
        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu);
            var doc = docu as FreeDocument;

            if (doc.Children != null)
            {
                var items = doc.Children;

                foreach (var item in items)
                {
                    var proces = new ProcessTask();
                    proces.Project = this;
                    proces.DictDeserialize(item);

                    Tasks.Add(proces);
                }
            }

            if (docu["DBConnections"] != null)
            {
                var items = docu["DBConnections"] as FreeDocument;

                if (items?.Children != null)
                    foreach (var item in items.Children)
                    {
                        var type = item["TypeName"].ToString();
                        var conn = PluginProvider.GetObjectByType<IDataBaseConnector>(type) as DBConnectorBase;
                        if (conn == null) continue;
                        conn.DictDeserialize(item);

                        DBConnections.Add(conn);
                    }
            }
            if (docu["RunningTasks"] != null)
            {
                var tasks = docu["RunningTasks"] as FreeDocument;


                SavedRunningTasks = tasks?.Children;

            }

            if (docu["Parameters"] != null)
            {
                var tasks = docu["Parameters"] as FreeDocument;

                if (tasks != null)
                {
                    Parameters.AddRange(tasks?.Children?.Select(d =>
                    {
                        var param = new ParameterItem();
                        param.UnsafeDictDeserialize(d);
                        return param;
                    }));

                }
        

            }

            ConfigSelector.SelectItem =
                docu["ParameterName"].ToString();

            if (DBConnections.FirstOrDefault(d => d.TypeName == GlobalHelper.Get("FileManager")) == null)
            {
                var filemanager = new FileManager {Name = GlobalHelper.Get("key_310")};
                DBConnections.Add(filemanager);
            }
            if (DBConnections.FirstOrDefault(d => d.TypeName == "MongoDB") == null)
            {
                var mongo = new MongoDBConnector {Name = "MongoDB连接器"};
                mongo.DBName = "hawk";
                DBConnections.Add(mongo);
            }
            if (DBConnections.FirstOrDefault(d => d.TypeName == GlobalHelper.Get("SQLiteDatabase")) == null)
            {
                var sqlite = new SQLiteDatabase {Name = GlobalHelper.Get("SQLiteDatabase")};
                sqlite.DBName = "hawk-sqlite";
                DBConnections.Add(sqlite);
            }
        }
    }
}