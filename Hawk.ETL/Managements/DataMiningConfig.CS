using System.Collections.Generic;
using System.Linq;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Managements
{
    /// <summary>
    ///     数据挖掘系统配置和常用参数类
    ///     基础操作文件
    /// </summary>
    [XFrmWork("数据挖掘配置" )]
    public class DataMiningConfig : ConfigFile
    {
        #region Constants and Fields

        public DataMiningConfig()
        {
            Projects = new List<ProjectItem>();
        }

        #endregion

        public override string SavePath => GetXMLFileLocation("MainConfig");


        public List<ProjectItem> Projects { get; set; }

        #region Constructors and Destructors

        public override void RebuildConfig()
        {
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            FreeDocument dict = base.DictSerialize(scenario);
            dict.Children = new List<FreeDocument>();
            dict.Children.AddRange(Projects.Select(d => d.DictSerialize(scenario)));
            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(dicts, scenario);
            var doc = dicts as FreeDocument;
            if (doc == null || doc.Children == null) return;
            foreach (FreeDocument child in doc.Children)
            {
                var project = new ProjectItem();

                project.DictDeserialize(child);
                Projects.Add(project);
            }
        }

        #endregion

        #region Public Methods

        public static string GetXMLFileLocation(string fileName)
        {
            //  if (MainDescription.IsUIForm)
            return MainDescription.MainFrm.MainPluginLocation + "\\" + fileName + ".xml";
        }

        public static string GetFileLocation(string fileName)
        {
            // if (MainDescription.IsUIForm)
            {
                return MainDescription.MainFrm.MainPluginLocation + "\\" + fileName;
            }
        }


        public static string GetFolderLocaion(string folderName)
        {
            // if (MainDescription.IsUIForm)
            {
                return MainDescription.MainFrm.MainPluginLocation + "\\" + folderName + "\\";
            }
        }

        #endregion

        // Public Methods (3) 
    }
}