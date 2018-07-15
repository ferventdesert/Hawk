using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Xml.Serialization;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
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

        [XmlIgnore]
        [Browsable(false)]

        public string TypeName => ProcessToDo?["Type"].ToString()=="SmartETLTool"?GlobalHelper.Get("key_201") :GlobalHelper.Get("key_202");

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

        public virtual void Load(bool addui)
        {
            if (
                (ProcessManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == this.Name) == null).SafeCheck(GlobalHelper.Get("key_326")) ==
                false)
                return;
            ControlExtended.SafeInvoke(() =>
            {
                var processname = ProcessToDo["Type"].ToString();
                if (string.IsNullOrEmpty(processname))
                    return;
                var process = ProcessManager.GetOneInstance(processname, newOne: true,addUI: addui);
                ProcessToDo.DictCopyTo(process as IDictionarySerializable);
                process.Init();
                EvalScript();
            }, LogType.Important, string.Format(GlobalHelper.Get("key_327"),Name), MainDescription.IsUIForm);
        }

        [LocalizedDisplayName("key_328")]
        [PropertyOrder(6)]
        public string ScriptPath { get; set; }

  

        #endregion
    }
}