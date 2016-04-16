using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Transformers;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Generators
{
    public class ETLBase : IColumnProcess
    {
        protected readonly IProcessManager processManager;
        private string _etlSelector;
        private SmartETLTool mainstream;

        public ETLBase()
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
        protected bool IsExecute;

        public void SetExecute(bool value)
        {
            IsExecute = value;
        }

        protected SmartETLTool etl { get; set; }

        [DisplayName("插入模块名")]
        [Description("主stream中，插入到etl的stream之前的模块名")]
        public string Insert { get; set; }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = this.UnsafeDictSerialize();
            dict.Add("Type", GetType().Name);
            dict.Add("Group", "Transformer");
            return dict;
        }

        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserialize(docu);
        }

        [Category("1.基本选项"), PropertyOrder(1), DisplayName("原列名")]
        public string Column { get; set; }

        [Category("1.基本选项")]
        [DisplayName("模块名")]
        public string Name { get; set; }

        [Category("1.基本选项")]
        [DisplayName("启用")]
        [PropertyOrder(5)]
        public bool Enabled { get; set; }

        [Browsable(false)]
        public string TypeName
        {
            get
            {
                var item = AttributeHelper.GetCustomAttribute(GetType());
                if (item == null)
                    return GetType().ToString();
                return item.Name;
            }
        }

        public void Finish()
        {
        }

        public virtual bool Init(IEnumerable<IFreeDocument> datas)
        {
            mainstream =
                processManager.CurrentProcessCollections.OfType<SmartETLTool>()
                    .FirstOrDefault(d => d.CurrentETLTools.Contains(this));
            etl =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == ETLSelector) as SmartETLTool;
            if (etl != null)
            {
                return true;
            }

            var task = processManager.CurrentProject.Tasks.FirstOrDefault(d => d.Name == ETLSelector);
            if (task == null)
                return false;

            ControlExtended.UIInvoke(() => { task.Load(); });

            etl =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == ETLSelector) as SmartETLTool;
            etl.InitProcess(true);
            return etl != null;
        }

        public override string ToString()
        {
            return TypeName + " " + Column;
        }

        protected IEnumerable<IColumnProcess> GetProcesses()
        {
            var processes = new List<IColumnProcess>();
            if (string.IsNullOrWhiteSpace(Insert))
                return etl.CurrentETLTools.Where(d=>d.Enabled);
            var index = mainstream.CurrentETLTools.IndexOf(this);
            foreach (var tool in mainstream.CurrentETLTools.Take(index).Where(d => d.Name == Insert))
            {
                processes.Add(tool);
            }
            foreach (var tool in etl.CurrentETLTools)
            {
                if (tool.Enabled && tool.Name != Insert)
                {
                    processes.Add(tool);
                }
            }
            return processes;
        }
    }

    [XFrmWork("从ETL生成", "从其他数据清洗模块中生成序列，用以组合大模块")]
    public class EtlGE : ETLBase, IColumnGenerator
    {
        public int Position { get; set; }

        [DisplayName("生成模式")]
        public MergeType MergeType { get; set; }

        public bool IsExecute { get; set; }
        public IEnumerable<FreeDocument> Generate(IFreeDocument document = null)
        {
            var process = GetProcesses();

            foreach (var item in etl.Generate(process, IsExecute).Select(d => d as FreeDocument))
            {
                yield return item;
            }
        }

        public int? GenerateCount()
        {
            return null;
        }
    }

    
    [XFrmWork("ETL执行", "从其他数据清洗模块中生成序列，用以组合大模块")]
    public class EtlEX : ETLBase, IDataExecutor
    {
        private EnumerableFunc func;

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);
            var process = GetProcesses().ToList();
             func = etl.Aggregate(d => d, process, true);
            return true;

        }

        [Browsable(false)]
        public bool IsExecute { get; set; }
        public bool AddTask { get; set; }

        public string NewColumn { get; set; }
        public IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
          
            foreach (var document in documents)
            {
               
               
                IFreeDocument doc = null;
                if (string.IsNullOrEmpty(NewColumn))
                {
                    doc= document.Clone();
                }
                else
                {
                    doc=new FreeDocument();
                    doc.MergeQuery(document, NewColumn+" "+Column);
                }
                if (AddTask)
                {
                    var name = doc[Column];

                    var task = TemporaryTask.AddTempTask("ETL" + name, func( new List<IFreeDocument> { doc }), d => d.ToList());
                     processManager.CurrentProcessTasks.Add(task);
                }
                else
                {
                  var r=  func( new List<IFreeDocument> {doc}).ToList();
                }
              
                yield return document;
            }
        }
    }
    [XFrmWork("字典转换")]
    public class DictTF : TransformerBase
    {
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }

        public string ValueColumn { get; set; }

   
        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            if (string.IsNullOrEmpty(Column) || string.IsNullOrEmpty(ValueColumn))
            {
                foreach (var data in datas)
                {
                    yield return data;
                }
                yield break;
            }
            var result = new FreeDocument();
            foreach (var data in datas)
            {
                var key = data[Column].ToString();
                var value = data[ValueColumn].ToString();
               
                if(string.IsNullOrEmpty(key)&&string.IsNullOrEmpty(value))
                {
                    yield return result.Clone();
                }
                else
                {
                    result.SetValue(key, value);
                }
            }
          

        }
    }

    [XFrmWork("ETL转换", "从其他数据清洗模块中生成序列，用以组合大模块")]
    public class EtlTF : ETLBase, IColumnDataTransformer
    {
        private IEnumerable<IColumnProcess> process;
        private EnumerableFunc func;

        [Browsable(false)]
        public string NewColumn { get; set; }

        public bool OneOutput => false;

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);
            process = GetProcesses();
             func = etl.Aggregate(d => d, process, IsExecute);
            return true;
        }

        protected bool IsExecute;

        public void SetExecute(bool value)
        {
            IsExecute = value;
        }

        public object TransformData(IFreeDocument data)
        {
            var result = func(new List<IFreeDocument>() {data.Clone()}).FirstOrDefault();
            data.AddRange(result);
            return null;
        }

        public bool IsMultiYield { get; set; }

        public IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var result = etl.Generate(process, IsExecute, new List<IFreeDocument> {data.Clone()});
                foreach (var item in result)
                {
                    yield return item.MergeQuery(data, NewColumn);
                }
            }
        }
    }
}