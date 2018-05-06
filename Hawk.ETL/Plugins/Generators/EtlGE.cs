using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Transformers;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Generators
{
    public class SubTaskModel : PropertyChangeNotifier
    {
        private int _rangeEnd;
        private int _rangeStart;
        private Window parent;

        public SubTaskModel(SmartETLTool mother, SmartETLTool subTask, ETLBase etlmodule,string[] mother_keys=null,string[] sub_keys=null)
        {
            MappingPairs =
                new ObservableCollection<MappingPair>();
            Mother = mother;
            SubTask = subTask;
            var start = 0;
            var end = 0;
            ETLBase.GetRange(etlmodule.ETLRange, subTask.AllETLMount, out start, out end);
            if (mother_keys!=null)
               motherkeys.AddRange(mother_keys); 
            if (sub_keys != null)
               subkeys.AddRange(sub_keys); 
            RangeStart = start;
            RangeEnd = end;
            ETLModule = etlmodule;
            if (!string.IsNullOrEmpty(etlmodule.MappingSet))
            {
                foreach (var item in etlmodule.MappingSet.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = item.Split(':');
                    string key = "";
                    string value = "";
                    if (kv.Length ==1)
                        key = value = kv[1];
                    if (kv.Length > 1)
                    {
                        key = kv[0];
                        value = kv[1];
                    }
                    var pair = new MappingPair(motherkeys,subkeys);
                    pair.Source._SelectItem = key;
                    pair.Target._SelectItem = value;
                    MappingPairs.Add(pair);
                }
            }
           // Refresh();

        }

        public ETLBase ETLModule { get; set; }

        public int RangeStart
        {
            get { return _rangeStart; }
            set
            {
                if (_rangeStart != value)
                {
                    _rangeStart = value;
                    OnPropertyChanged("RangeStart");
                }
            }
        }

        public int RangeEnd
        {
            get { return _rangeEnd; }
            set
            {
                if (_rangeEnd != value)
                {
                    _rangeEnd = value;
                    OnPropertyChanged("RangeEnd");
                }
            }
        }

        public string ETLRange
        {
            get
            {
                int end = RangeEnd;
                if (end >= this.SubTask.CurrentETLTools.Count)
                    end = 300; 
                return $"{RangeStart}:{end}";
            }
        }

        public string MappingSet
        {
            get { return " ".Join(MappingPairs.Select(d => $"{d.Source.SelectItem}:{d.Target.SelectItem}")); }
        }

        public SmartETLTool Mother { get; set; }
        public SmartETLTool SubTask { get; set; }

        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {

                        new Command("刷新", async obj => await Mother.MainFrm.RunBusyWork(Refresh), icon: "refresh"),
                        new Command("确认结果", obj =>
                        {
                            parent.DialogResult = true;

                            parent.Close();
                        }, icon: "check"),
                        new Command("退出", obj =>
                        {
                            parent.DialogResult = false;
                            parent.Close();
                        }, icon: "close"),
                        new Command("添加", obj =>
                        {
                            var pair = new MappingPair(motherkeys,subkeys);
                            MappingPairs.Add(pair);
                            
                        }, icon: "add"),
                        new Command("删除", obj => { MappingPairs.Remove(obj as MappingPair); }, obj => obj is MappingPair,
                            "add")
                    }
                    );
            }
        }

        public ObservableCollection<MappingPair> MappingPairs { get; set; }

        public void SetView(object view, Window window)
        {
            parent = window;
        }

        private List<string> motherkeys=new List<string>();
        private List<string> subkeys=new List<string>(); 
        private bool Refresh()
        {

            //motherkeys = this.Mother.Generate(Mother.CurrentETLTools.Take(Mother.CurrentETLTools.IndexOf(this.ETLModule)), false).Take(3).GetKeys().ToList();
            //     subkeys =
            //        SubTask.Generate(
            //            SubTask.CurrentETLTools.Take(RangeStart),false)
            //            .Take(3).GetKeys().ToList();
                var dict =
                    MappingPairs.Where(
                        d =>
                            string.IsNullOrEmpty(d.Target.SelectItem) == false &&
                            string.IsNullOrEmpty(d.Source.SelectItem) == false).Distinct().GroupBy(d=>d.Target.SelectItem).Select(group=>group.First())
                        .ToDictionary(d => d.Target.SelectItem, d => d.Source.SelectItem);
            ControlExtended.UIInvoke(() =>
            {
                MappingPairs.Clear();
                foreach (var key in subkeys)
                {
                    var pair = new MappingPair(motherkeys,subkeys);
                    pair.Target.SelectItem = key;
                    string value = key;
                    dict.TryGetValue(key, out value);
                    pair.Source.SelectItem =  value;
                    MappingPairs.Add(pair);

                }
            });
            return true;

            //var index = Mother.CurrentETLTools.IndexOf(ETLModule);
            //if (index == -1)
            //    return;
            //Mother.Generate(Mother.CurrentETLTools.Take(index), false);
        }

        public class MappingPair
        {
            public MappingPair(List<string> source_keys, List<string>target_keys)
            {
                Source = new TextEditSelector();
                Target = new TextEditSelector();
                Source.SetSource(source_keys);
                Target.SetSource(target_keys);
               
            }

            public TextEditSelector Source { get; set; }
            public TextEditSelector Target { get; set; }
        }
    }

    public class ETLBase : ToolBase, INotifyPropertyChanged
    {
        protected readonly IProcessManager processManager;
        private string _etlSelector;

        public ETLBase()
        {
            processManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;
            ETLSelector = new TextEditSelector {GetItems = this.GetAllETLNames()};
            ETLRange = "";
            Column = "column";
            Enabled = true;
            MappingSet = "";
        }

        [LocalizedCategory("2.调用选项")]
        [PropertyOrder(1)]
        [LocalizedDisplayName("图形化配置")]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("配置", obj => SetConfig(), obj => !string.IsNullOrEmpty(ETLSelector.SelectItem),
                            "refresh")
                    }
                    );
            }
        }

        [LocalizedCategory("2.调用选项")]
        [LocalizedDisplayName("子任务-选择")]
        [PropertyOrder(0)]
        [LocalizedDescription("输入或选择调用的子任务的名称")]
        public TextEditSelector ETLSelector { get; set; }

        [LocalizedCategory("2.调用选项")]
        [LocalizedDisplayName("调用范围")]
        [PropertyOrder(2)]
        [LocalizedDescription(
            "设定调用子任务的模块范围，例如2:30表示被调用任务的第2个到第30个子模块将会启用，其他模块忽略，2:-1表示从第2个到倒数第二个启用，符合python的slice语法，为空则默认全部调用"
            )]
        public string ETLRange { get; set; }

        [LocalizedCategory("2.调用选项")]
        [LocalizedDisplayName("属性映射")]
        [PropertyOrder(3)]
        [LocalizedDescription("源属性:目标属性列 多个映射中间用空格分割，例如A:B C:D, 表示主任务中的A,B属性列会以C,D的名称传递到子任务中")]
        public string MappingSet { get; set; }
     

        protected SmartETLTool etl { get; set; }

        private void SetConfig()
        {
            Init(null);
            var subTaskModel = new SubTaskModel(Father, etl, this,this.Father.Documents.GetKeys().ToArray(),etl.Documents.GetKeys().ToArray());
            var view = PluginProvider.GetObjectInstance<ICustomView>("子任务面板") as UserControl;
            view.DataContext = subTaskModel;

            var name = "设置子任务调用属性";
            var window = new Window {Title = name};
            window.Content = view;
            subTaskModel.SetView(view, window);
            window.Activate();
            window.ShowDialog();
            if (window.DialogResult == true)

            {
                ETLRange = subTaskModel.ETLRange;
                MappingSet = subTaskModel.MappingSet;
                OnPropertyChanged("ETLRange");
                OnPropertyChanged("MappingSet");
            }
        }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            if (string.IsNullOrEmpty(ETLSelector.SelectItem))
                return false;
            etl = this.GetModule<SmartETLTool>(ETLSelector.SelectItem);
            etl?.InitProcess(true);
            return etl != null;
        }

        public IFreeDocument MappingDocument(IFreeDocument doc)
        {
            if (doc == null)
                return null;
            if (string.IsNullOrEmpty(MappingSet))
                return doc;
            var newdoc = new FreeDocument();
            doc.DictCopyTo(newdoc);
            foreach (var item  in MappingSet.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = item.Split(':');
                if (kv.Length != 2)
                    continue;
                if(kv[0]==kv[1])
                    continue;
                if (newdoc.Keys.Contains(kv[0]))
                {
                    newdoc[kv[1]] = newdoc[kv[0]];
                    newdoc.Remove(kv[0]);
                }
            }
            return newdoc;
        }

        internal static bool GetRange(string strrange, int total, out int start, out int end)
        {
            start = 0;
            end = total;
            if (string.IsNullOrEmpty(strrange))
                return false;
            var range = strrange.Split(':');
            if (range.Length == 2)
            {
                try
                {
                    start = int.Parse(range[0]);
                    if (start < 0)
                        start = total + start;
                    end = int.Parse(range[1]);
                    if (end < 0)
                        end = total + end;
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error("子任务范围表达式错误，请检查:" + ex.Message);
                }
            }
            return true;
        }

        protected IEnumerable<IColumnProcess> GetProcesses()
        {
            var start = 0;
            var end = etl.CurrentETLTools.Count;
            GetRange(ETLRange, etl.CurrentETLTools.Count, out start, out end);
            if (etl == null)
                yield break;
            foreach (var tool in etl.CurrentETLTools.Skip(start).Take(end - start))
            {
                yield return tool;
            }
        }
    }

    [XFrmWork("子任务-生成", "调用其他任务作为生成器，使用类似于“生成区间数”")]
    public class EtlGE : ETLBase, IColumnGenerator
    {
        [LocalizedDisplayName("生成模式")]
        public MergeType MergeType { get; set; }

        public IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            var process = GetProcesses().ToList();
            if (process.Any() == false)
            {
                return new List<IFreeDocument>();
            }
            var documents = new List<IFreeDocument>();
            if (document != null)
                documents.Add(MappingDocument(document));

            return process.Generate( IsExecute, documents);
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


    [XFrmWork("子任务-执行", "调用其他任务，作为执行器块")]
    public class EtlEX : ETLBase, IDataExecutor
    {
        private EnumerableFunc func;

        [LocalizedDisplayName("添加到任务")]
        [LocalizedDescription("勾选后，本子任务会添加到任务管理器中")]
        public bool AddTask { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);
            var process = GetProcesses().ToList();
            func = process.Aggregate(isexecute: true);
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
                var doc = MappingDocument(document);
                if (AddTask)
                {
                    var name = doc[Column];
                    ControlExtended.UIInvoke(() =>
                    {
                        var task = TemporaryTask.AddTempTask("ETL" + name, func(new List<IFreeDocument> {doc}),
                            d => d.ToList());
                        processManager.CurrentProcessTasks.Add(task);
                    });
                }
                else
                {
                    var r = func(new List<IFreeDocument> {doc}).ToList();
                }

                yield return document;
            }
        }
    }

    [XFrmWork("矩阵转置", "将列数据转换为行数据，拖入的列为key","transform_rotate_right")]
    public class DictTF : TransformerBase
    {
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            if (string.IsNullOrEmpty(Column))
            {
                foreach (var data in datas)
                {
                    yield return data;
                }
                yield break;
            }
            var hasyield = false;
            var results = datas.ToList();
            var columns = results.Select(d => d[Column].ToString()).ToList();
            var all_keys = results.GetKeys(count: 100).ToList();
            var docs = new List<FreeDocument>();
            for (var i = 0; i < all_keys.Count(); i++)
            {
                docs.Add(new FreeDocument());
            }
            var pos = 0;
            foreach (var column in columns)
            {
                var pos2 = 0;
                foreach (var doc in docs)
                {
                    doc[column] = results[pos][all_keys[pos2++]];
                }
                pos += 1;
            }
            foreach (var doc in docs)
            {
                yield return doc;
            }
            //数字列可能会有不显示的问题
        }
    }

    [XFrmWork("子任务-转换", "调用所选的子任务作为转换器，有关子任务，请参考相关文档")]
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

        [LocalizedDisplayName("工作模式")]
        [LocalizedDescription("当要输出多个结果时选List，否则选One,参考“网页采集器”")]
        public ScriptWorkMode IsManyData { get; set; }

        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("输出列")]
        [LocalizedDescription("从原任务中传递到子任务的列，多个列用空格分割")]
        public string NewColumn { get; set; }

        [Browsable(false)]
        public bool OneOutput => false;

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);
            process = GetProcesses();
            func = process.Aggregate(isexecute: IsExecute);
            return true;
        }

        public object TransformData(IFreeDocument data)
        {
            var doc = MappingDocument(data);
            var result = func(new List<IFreeDocument> {doc}).FirstOrDefault();
            data.Clear();
            data.AddRange(result);
            return null;
        }

        [Browsable(false)]
        public bool IsMultiYield => IsManyData == ScriptWorkMode.List;

        public IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var doc = MappingDocument(data);
                if (IsCycle)
                {
                    var newdata = doc;
                    while (string.IsNullOrEmpty(newdata[Column].ToString()) == false)
                    {
                        var result =
                            process.Generate( IsExecute, new List<IFreeDocument> {newdata.Clone()}).FirstOrDefault();
                        if (result == null)
                            break;
                        yield return result.Clone();
                        newdata = result;
                    }
                }
                else
                {
                    var result = process.Generate( IsExecute, new List<IFreeDocument> {doc});
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