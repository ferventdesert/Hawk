using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Xml.Serialization;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;


namespace Hawk.ETL.Managements
{
 

    public class ProcessTask : TaskBase, IDictionarySerializable
    {
        #region Constants and Fields


        #endregion

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

        #endregion

        #region Public Methods

        public virtual void Load()
        {
            if (
                (ProcessManager.CurrentProcessTasks.FirstOrDefault(d => d == this) == null).SafeCheck("不能重复加载该任务") == false)
                return;
            ControlExtended.SafeInvoke(() =>
            {
              
                var processname = ProcessToDo["Type"].ToString();
                if(string.IsNullOrEmpty(processname))
                    return;
                var process=ProcessManager.GetOneInstance(processname,newOne:true) as IDictionarySerializable;
                ProcessToDo.DictCopyTo(process);
                
            }, LogType.Important, $"加载{Name}任务",true);
        }

       
        #endregion

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            FreeDocument docu = ProcessToDo.DictSerialize(scenario);
            docu.SetValue("CreateTime", CreateTime);
            
            docu.SetValue("Name", Name);
            docu.SetValue("Description", Description);
            return docu;
        }

      

        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            ProcessToDo.DictDeserialize(docu, scenario);
            Name = docu.Set("Name", Name);
            Description = docu.Set("Description", Description);
            CreateTime = docu.Set("CreateTime",CreateTime);
        }
     
    }
}