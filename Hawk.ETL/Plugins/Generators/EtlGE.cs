using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
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
        protected bool IsExecute;
        [Browsable(false)]
        public int ETLIndex { get; set; }


        public ETLBase()
        {
            processManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;
            ETLSelector.GetItems = this.GetAllETLNames();
            ETLRange = "";
            Column = "column";
            Enabled = true;
        }

        [LocalizedCategory("2.调用选项")]
        [LocalizedDisplayName("子流-选择")]
        [PropertyOrder(0)]
        [LocalizedDescription("输入要调用的子流的名称")]
        public TextEditSelector ETLSelector { get; set; }

        [LocalizedCategory("2.调用选项")]
        [LocalizedDisplayName("调用范围")]
        [PropertyOrder(1)]
        [LocalizedDescription("设定调用子流的模块范围，例如2:30表示从第2个到第30个子模块将会启用，其他的模块不启用，2:-1表示从第3个到倒数第二个启用，其他不启用，符合python的slice语法")]
        public string ETLRange { get; set; }


        protected SmartETLTool etl { get; set; }

        public void SetExecute(bool value)
        {
            IsExecute = value;
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = this.UnsafeDictSerializePlus();
            dict.Add("Type", GetType().Name);
            dict.Remove("ETLIndex");
            return dict;
        }


        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserialize(docu);
        }

        [LocalizedCategory("1.基本选项"), PropertyOrder(1), DisplayName("输入列")]
        public string Column { get; set; }


        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("启用")]
        [PropertyOrder(5)]
        public bool Enabled { get; set; }


        [LocalizedDisplayName("介绍")]
        [PropertyOrder(100)]
        [PropertyEditor("CodeEditor")]
        public string Description
        {
            get
            {
                var item = AttributeHelper.GetCustomAttribute(GetType());
                if (item == null)
                    return GetType().ToString();
                return item.Description;
            }
        }

        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("类型")]
        [PropertyOrder(0)]
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
            if (string.IsNullOrEmpty(ETLSelector.SelectItem))
                return false;
            etl =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == ETLSelector.SelectItem) as SmartETLTool;
            if (etl != null)
            {
                return true;
            }

            var task = processManager.CurrentProject.Tasks.FirstOrDefault(d => d.Name == ETLSelector.SelectItem);
            if (task == null)

            {
                throw new NullReferenceException($"can't find a ETL Module named {ETLSelector}");
            }

            ControlExtended.UIInvoke(() => { task.Load(false); });



            etl.InitProcess(true);
            return etl != null;
        }

        public override string ToString()
        {
            return TypeName + " " + Column;
        }

        protected IEnumerable<IColumnProcess> GetProcesses()
        {

            var range = ETLRange.Split(':');
            var start = 0;
            if (etl == null)
                yield break;

            var end = etl.CurrentETLTools.Count;
            if (range.Length == 2)
            {
                try
                {
                    start = int.Parse(range[0]);
                    if (start < 0)
                        start = etl.CurrentETLTools.Count + start;
                    end = int.Parse(range[1]);
                    if (end < 0)
                        end = etl.CurrentETLTools.Count + end;
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error("子流范围表达式错误，请检查:" + ex.Message);
                }
            }
            foreach (var tool in etl.CurrentETLTools.Skip(start).Take(end - start))
            {
                yield return tool;
            }
        }
    }

    [XFrmWork("子流-生成", "从其他数据清洗模块中生成序列，用以组合大模块")]
    public class EtlGE : ETLBase, IColumnGenerator
    {


        [LocalizedDisplayName("生成模式")]
        public MergeType MergeType { get; set; }

        public IEnumerable<FreeDocument> Generate(IFreeDocument document = null)
        {
            var process = GetProcesses().ToList();
            if (process.Any() == false)
            {
                yield break;
            }
            foreach (var item in etl.Generate(process, IsExecute).Select(d => d as FreeDocument))
            {
                yield return item;
            }
        }

        public int? GenerateCount()
        {
            return null;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var data = base.DictSerialize(scenario);
            data.SetValue("Group", "Generator");
            return data;
        }
    }


    [XFrmWork("子流-执行", "从其他数据清洗模块中生成序列，用以组合大模块")]
    public class EtlEX : ETLBase, IDataExecutor
    {
        private EnumerableFunc func;

        [LocalizedDisplayName("添加到任务")]
        [LocalizedDescription("勾选后，本子任务会添加到任务管理器中")]
        public bool AddTask { get; set; }


          [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("输出列")]
        [LocalizedDescription("从原始数据中传递到子执行流的列，多个列用空格分割")]
        public string NewColumn { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);
            var process = GetProcesses().ToList();
            func = etl.Aggregate(d => d, process, true);
            return true;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var data = base.DictSerialize(scenario);
            data.SetValue("Group", "Executor");
            return data;
        }

        public IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            foreach (var document in documents)
            {
                IFreeDocument doc = null;
                if (string.IsNullOrEmpty(NewColumn))
                {
                    doc = document.Clone();
                }
                else
                {
                    doc = new FreeDocument();
                    doc.MergeQuery(document, NewColumn + " " + Column);
                }
                if (AddTask)
                {
                    var name = doc[Column];
                    ControlExtended.UIInvoke(() =>
                    {
                        var task = TemporaryTask.AddTempTask("ETL" + name, func(new List<IFreeDocument> { doc }),
                            d => d.ToList());
                        processManager.CurrentProcessTasks.Add(task);
                    });
                }
                else
                {
                    var r = func(new List<IFreeDocument> { doc }).ToList();
                }

                yield return document;
            }
        }
    }

    [XFrmWork("字典转换", "将两列数据，转换为一行数据，拖入的列为key")]
    public class DictTF : TransformerBase
    {
        [LocalizedDisplayName("值列名")]
        public string ValueColumn { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }

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
            var hasyield = false;
            var result = new FreeDocument();
            foreach (var data in datas)
            {
                var key = data[Column]?.ToString();
                var value = data[ValueColumn]?.ToString();

                if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value))
                {
                    yield return result.Clone();
                    hasyield = true;
                }
                else
                {
                    result.SetValue(key, value);
                }
            }
            if (hasyield == false)
                yield return result.Clone();


        }
    }

    [XFrmWork("子流-转换", "从其他数据清洗模块中生成序列，用以组合大模块")]
    public class EtlTF : ETLBase, IColumnDataTransformer
    {
        private EnumerableFunc func;
        private IEnumerable<IColumnProcess> process;

        public EtlTF()
        {
            NewColumn = "";
            IsCycle = false;
        }

        [LocalizedDisplayName("递归到下列")]
        public bool IsCycle { get; set; }

        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("输出列")]
        [LocalizedDescription("从原始数据中传递到子执行流的列，多个列用空格分割")]
        public string NewColumn { get; set; }

        [Browsable(false)]
        public bool OneOutput => false;

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);
            process = GetProcesses();
            func = etl.Aggregate(d => d, process, IsExecute);
            return true;
        }

        public object TransformData(IFreeDocument data)
        {
            var result = func(new List<IFreeDocument> { data.Clone() }).FirstOrDefault();
            data.AddRange(result);
            return null;
        }

        [LocalizedDisplayName("返回多个数据")]
        public bool IsMultiYield { get; set; }

        public IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                if (IsCycle)
                {
                    var newdata = data;
                    while (string.IsNullOrEmpty(newdata[Column].ToString()) == false)
                    {
                        var result =
                            etl.Generate(process, IsExecute, new List<IFreeDocument> { newdata.Clone() }).FirstOrDefault();
                        if (result == null)
                            break;
                        yield return result.Clone();
                        newdata = result;
                    }
                }
                else
                {
                    var result = etl.Generate(process, IsExecute, new List<IFreeDocument> { data.Clone() });
                    foreach (var item in result)
                    {
                        yield return item.MergeQuery(data, NewColumn);
                    }
                }
            }
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var data = base.DictSerialize(scenario);
            data.SetValue("Group", "Transformer");
            return data;
        }
    }
}