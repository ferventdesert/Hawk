using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Xml.Serialization;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;
using IronPython.Hosting;
using Microsoft.Scripting;

namespace Hawk.ETL.Managements
{
    public class ProcessTask : TaskBase, IDictionarySerializable
    {
        #region Constructors and Destructors

        public ProcessTask()
        {
            ProcessToDo = new FreeDocument();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     要实现的算法和对应的配置
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public FreeDocument ProcessToDo { get; set; }


        public string TaskType
        {
            get
            {
                if(ProcessToDo!=null)
                    return ProcessToDo["Type"].ToString();
                return null;
            }
        }
        [XmlIgnore]
        [Browsable(false)]

        public string TypeName => ProcessToDo?["Type"].ToString()=="SmartETLTool"?GlobalHelper.Get("smartetl_name") :GlobalHelper.Get("smartcrawler_name");

        #endregion

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var docu = ProcessToDo.DictSerialize(scenario);
            docu.SetValue("Name", Name);
            docu.SetValue("Description", Description);
            docu.SetValue("ScriptPath", ScriptPath);
            return docu;
        }

        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            ProcessToDo.DictDeserialize(docu, scenario);
            Name = docu.Set("Name", Name);
            Description = docu.Set("Description", Description);
            ScriptPath = docu.Set("ScriptPath", ScriptPath);
        }

        #region Constants and Fields

        #endregion

        #region Public Methods
        [Browsable(false)]
        public Project Project { get; set; }

        public void EvalScript( )
        {
            if(string.IsNullOrWhiteSpace(ScriptPath))
                return;
            var script = ScriptPath;
           
            var path = Project.SavePath;
            XLogSys.Print.DebugFormat(GlobalHelper.Get("key_322"),Project.SavePath);
            var folder = new DirectoryInfo(path).Parent?.FullName;
            if (folder != null)
                script = folder +"\\"+ script;

            if (!File.Exists(script))
            {
                XLogSys.Print.WarnFormat(GlobalHelper.Get("key_323"),Project.Name,ScriptPath);
                return;
            }
               

            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();

            try
            {
                var source = engine.CreateScriptSourceFromFile(script);
                var compiledCode = source.Compile();
                foreach (var process in ProcessManager.CurrentProcessCollections)
                {
                    scope.SetVariable(process.Name, process);
                }
                dynamic d;

                d = compiledCode.Execute(scope);
            }
            catch (Exception ex)
            {
                var syntax = ex as SyntaxErrorException;
                if (syntax != null)
                {
                    XLogSys.Print.ErrorFormat(GlobalHelper.Get("key_324"),ex.Message,syntax.Line, syntax.RawSpan.Start,syntax.RawSpan.End);
                    return;
                }
                XLogSys.Print.Error(ex);
            }
            XLogSys.Print.Info(GlobalHelper.Get("key_325"));
        }

        public virtual IDataProcess Load(bool addui)
        {
            IDataProcess process=ProcessManager.GetTask(this.TaskType, this.Name);
            if(process!=null)
                return process;
            ControlExtended.SafeInvoke(() =>
            {
                process = ProcessManager.GetOneInstance(this.TaskType, newOne: true,addUI: addui);
                ProcessToDo.DictCopyTo(process as IDictionarySerializable);
                process.Init();
                EvalScript();
            }, LogType.Important, string.Format(GlobalHelper.Get("key_327"),Name), MainDescription.IsUIForm);
            return process;
        }

        [LocalizedDisplayName("key_328")]
        [PropertyOrder(6)]
        public string ScriptPath { get; set; }

  

        #endregion
    }
}