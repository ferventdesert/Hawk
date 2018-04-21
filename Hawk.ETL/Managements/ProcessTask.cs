using System;
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

        public string TypeName => ProcessToDo?["Type"].ToString()=="SmartETLTool"?"数据清洗":"网页采集器";

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
            XLogSys.Print.DebugFormat("加载工程文件，位置为{0}",Project.SavePath);
            var folder = new DirectoryInfo(path).Parent?.FullName;
            if (folder != null)
                script = folder +"\\"+ script;

            if (!File.Exists(script))
            {
                XLogSys.Print.WarnFormat("加载{0}工程时未发现对应的脚本文件{1}",Project.Name,ScriptPath);
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
                    XLogSys.Print.ErrorFormat("编译错误：{0}，位置在{1}行,从{2}到{3}",ex.Message,syntax.Line, syntax.RawSpan.Start,syntax.RawSpan.End);
                    return;
                }
                XLogSys.Print.Error(ex);
            }
            XLogSys.Print.Info("脚本已经成功执行");
        }

        public virtual void Load(bool addui)
        {
            if (
                (ProcessManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == this.Name) == null).SafeCheck("不能重复加载该任务") ==
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
            }, LogType.Important, $"加载{Name}任务", MainDescription.IsUIForm);
        }

        [LocalizedDisplayName("脚本路径")]
        [PropertyOrder(6)]
        public string ScriptPath { get; set; }

  

        #endregion
    }
}