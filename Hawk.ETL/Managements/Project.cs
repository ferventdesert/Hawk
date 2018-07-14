using System;
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

namespace Hawk.ETL.Managements
{
    public class ProjectItem : PropertyChangeNotifier, IDictionarySerializable
    {
        [LocalizedDisplayName("保存路径")]
        public string SavePath { get; set; }

        [LocalizedDisplayName("名称")]
        public string Name { get; set; }

        [LocalizedDisplayName("描述")]
        public string Description { get; set; }

        [LocalizedDisplayName("版本")]
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

        public static Project LoadProject(string path)
        {
            if (File.Exists(path) == false)
                throw new Exception("当前工程文件的路径不存在，生成新工程");
            var xml = new FileConnectorXML {FileName = path};
            var r = xml.ReadFile().FirstOrDefault();
            var project = new Project();

            if (r != null)
            {
                project.DictDeserialize(r.DictSerialize());
            }
            project.SavePath = path;
            return project;
        }
    }

    /// <summary>
    ///     项目信息
    /// </summary>
    public class Project : ProjectItem
    {
        public Project()
        {
            Tasks = new ObservableCollection<ProcessTask>();
            DBConnections = new ObservableCollection<IDataBaseConnector>();
            ;

        }



        [LocalizedDisplayName("数据库连接")]
        public ObservableCollection<IDataBaseConnector> DBConnections { get; set; }


        /// <summary>
        ///     在工程中保存的所有任务
        /// </summary>
        [LocalizedDisplayName("任务列表")]
        public ObservableCollection<ProcessTask> Tasks { get; set; }


     

        public void Save()
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

            connector.WriteAll(
                new List<IFreeDocument> {DictSerialize()}
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
                    XLogSys.Print.Error($"文件{connector.FileName}不存在");
                    return null;
                }
             
            }
            var proj = connector.ReadFile().FirstOrDefault();

            if (proj == null)
                return null;
            var proj2 = new Project();
            proj.DictCopyTo(proj2);
            proj2.SavePath = connector.FileName;

            return proj2;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize();

            dict.Children = Tasks.Select(d => d.DictSerialize()).ToList();
            var connecots = new FreeDocument
            {
                Children = DBConnections.Select(d => (d as IDictionarySerializable).DictSerialize()).ToList()
            };
            ;
            dict.Add("DBConnections", connecots);
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
            if (DBConnections.FirstOrDefault(d => d.TypeName == "文件管理") == null)
            {
                var filemanager = new FileManager() {Name = "最近打开的文件"};
                DBConnections.Add(filemanager);
            }
            if (DBConnections.FirstOrDefault(d => d.TypeName == "MongoDB") == null)
            {
                var mongo = new MongoDBConnector() {Name = "MongoDB连接器"};
                mongo.DBName = "hawk";
                DBConnections.Add(mongo);

            }
            if (DBConnections.FirstOrDefault(d => d.TypeName == "SQLite数据库") == null)
            {
                var sqlite = new SQLiteDatabase() { Name = "SQLite数据库" };
                sqlite.DBName = "hawk-sqlite";
                DBConnections.Add(sqlite);

            }
        }
    }
}