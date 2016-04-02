using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Generators
{

    [XFrmWork("从ETL生成","从其他数据清洗模块中生成序列，用以组合大模块")]
  public  class EtlGE: GeneratorBase
    {
        private readonly IProcessManager processManager;
        private string _etlSelector;
        private SmartETLTool mainstream;

        public EtlGE()
        {
            processManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;
            var defaultetl = processManager.CurrentProcessCollections.FirstOrDefault(d => d is SmartETLTool);
            if (defaultetl != null) ETLSelector = defaultetl.Name;
        }
        [DisplayName("ETL选择")]
        [Description("输入ETL的任务名称")]
        public string ETLSelector
        {
            get { return _etlSelector; }
            set
            {
                if (_etlSelector != value)
                {
                }
                _etlSelector = value;
            }
        }
     
        private SmartETLTool etl { get; set; }

        [DisplayName("插入模块名")]
        [Description("主stream中，插入到etl的stream之前的模块名")]
        public string Insert { get; set; }

       

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
             mainstream = processManager.CurrentProcessCollections.OfType<SmartETLTool>().FirstOrDefault(d => d.CurrentETLTools.Contains(this));
            etl =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == ETLSelector) as SmartETLTool;
            if (etl != null)
            {

                return etl != null && base.Init(datas);
            }

            var task = processManager.CurrentProject.Tasks.FirstOrDefault(d => d.Name == ETLSelector);
            if (task == null)
                return false;
             
            ControlExtended.UIInvoke(() => { task.Load(); });

            etl =
            processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == ETLSelector) as SmartETLTool;

            return etl != null && base.Init(datas);
        }

        public override IEnumerable<FreeDocument> Generate(IFreeDocument document = null)
        {

            var process = GetProcesses().Where(d => !(d is IDataExecutor));

            foreach (var item in etl.Generate(process).Select(d => d as FreeDocument))
            {
                yield return item;
            }
          
        }

        private IEnumerable<IColumnProcess> GetProcesses()
        {
            List<IColumnProcess>processes=new List<IColumnProcess>();
            if (string.IsNullOrWhiteSpace(Insert))
                return etl.CurrentETLTools;
            var index = mainstream.CurrentETLTools.IndexOf(this);
            foreach (var tool in mainstream.CurrentETLTools.Take(index).Where(d=>d.Name==Insert))
            {
                processes.Add(tool);
            }
            foreach (var tool in etl.CurrentETLTools)
            {
                if (tool.Enabled && tool.Name!= Insert)
                {
                    processes.Add(tool);
                }
            }
            return processes;

        } 
      
    }
}
