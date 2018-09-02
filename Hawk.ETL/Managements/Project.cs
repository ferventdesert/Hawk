using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Data;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;

namespace Hawk.ETL.Managements
{
    public class ProjectItem : PropertyChangeNotifier, IDictionarySerializable
    {
        [LocalizedDisplayName("key_329")]
        public string SavePath { get; set; }

        [LocalizedDisplayName("key_18")]
        public string Name { get; set; }

        [LocalizedDisplayName("key_16")]
        public string Description { get; set; }

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

    /// <summary>
    ///     项目信息
    /// </summary>
    public class Project : ProjectItem
    {
        private IProcessManager sysProcessManager;

        public Project()
        {
            Tasks = new ObservableCollection<ProcessTask>();
            DBConnections = new ObservableCollection<IDataBaseConnector>();
            Parameters=new Dictionary<string, string>();
             sysProcessManager = MainDescription.MainFrm.PluginDictionary["DataProcessManager"] as IProcessManager;

        }
        [Browsable(false)]
        public List<DataCollection> DataCollections { get; set; }

        [LocalizedDisplayName("key_332")]
        public ObservableCollection<IDataBaseConnector> DBConnections { get; set; }


        /// <summary>
        ///     在工程中保存的所有任务
        /// </summary>
        [LocalizedDisplayName("key_333")]
        public ObservableCollection<ProcessTask> Tasks { get; set; }

     
        [Browsable(false)]
        public Dictionary<string, string> Parameters { get; set; }
                          

        [LocalizedDisplayName("key_21")]
        [PropertyEditor("CodeEditor")]
        public string ParameterString
        {
            get
            {
                return "\n".Join( Parameters.Select(d=>d.Key+":"+d.Value));
                
            }
            set { Parameters = ExtendEnumerable.ToDict(value); }
        }

        public void Save(IEnumerable<DataCollection>collections=null)
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
            {
                dict["DataCollections"] = new FreeDocument()
                {
                    Children = collections.Where(d => d.Count < 100000).Select(d => d.DictSerialize()).ToList()
                };
            }
            connector.WriteAll(
                new List<IFreeDocument> {dict}
                );
        }

        public static Project Load( string path = null)
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
                    XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_334"),connector.FileName));
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
            if(projfile.TryGetValue("DataCollections",out collectionObj))
            {

                List<FreeDocument> collectionDocs = (collectionObj as FreeDocument)?.Children as List<FreeDocument>;
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

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize();

            dict.Children = Tasks.Select(d => d.DictSerialize()).ToList();
            var connectors = new FreeDocument
            {
                Children = DBConnections.Select(d => (d as IDictionarySerializable).DictSerialize()).ToList()
            };
            dict.Add("DBConnections", connectors);
       
            if (sysProcessManager != null)
            {
                var runningTasks = new FreeDocument
                {
                    Children =
                  sysProcessManager.CurrentProcessTasks.Where(d=>d.IsCanceled==false&&d.Publisher is SmartETLTool).OfType<TemporaryTask<FreeDocument>>().Select(d => d.DictSerialize())
                            .ToList()
                };
                dict.Add("RunningTasks", runningTasks);
            }
            return dict;
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
                {
                    foreach (var item in items.Children)
                    {
                        var type = item["TypeName"].ToString();
                        var conn = PluginProvider.GetObjectByType<IDataBaseConnector>(type) as DBConnectorBase;
                        if (conn == null) continue;
                        conn.DictDeserialize(item);

                        DBConnections.Add(conn);
                    }
                }

            }
            if (docu["RunningTasks"] != null)
            {
                var items = docu["RunningTasks"] as FreeDocument;

                if (items?.Children != null)
                {
                    foreach (var item in items.Children)
                    {
                        var conn =new TemporaryTask<FreeDocument> ();

                        //sysProcessManager.

                        //RunningTasks.Add(conn);
                    }
                }

            }
            if (DBConnections.FirstOrDefault(d => d.TypeName == GlobalHelper.Get("FileManager")) == null)
            {
                var filemanager = new FileManager() {Name = GlobalHelper.Get("key_310")};
                DBConnections.Add(filemanager);
            }
            if (DBConnections.FirstOrDefault(d => d.TypeName == "MongoDB") == null)
            {
                var mongo = new MongoDBConnector() {Name = "MongoDB连接器"};
                mongo.DBName = "hawk";
                DBConnections.Add(mongo);

            }
            if (DBConnections.FirstOrDefault(d => d.TypeName == GlobalHelper.Get("SQLiteDatabase")) == null)
            {
                var sqlite = new SQLiteDatabase() { Name = GlobalHelper.Get("SQLiteDatabase") };
                sqlite.DBName = "hawk-sqlite";
                DBConnections.Add(sqlite);

            }
        }
    }
}