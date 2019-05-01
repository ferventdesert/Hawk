using System;
using Hawk.Core.Utils;
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

                        new Command(GlobalHelper.Get("key_142"), async obj => await Mother.MainFrm.RunBusyWork(Refresh), icon: "refresh"),
                        new Command(GlobalHelper.Get("key_172"), obj =>
                        {
                            parent.DialogResult = true;

                            parent.Close();
                        }, icon: "check"),
                        new Command(GlobalHelper.Get("key_173"), obj =>
                        {
                            parent.DialogResult = false;
                            parent.Close();
                        }, icon: "close"),
                        new Command(GlobalHelper.Get("key_302"), obj =>
                        {
                            var pair = new MappingPair(motherkeys,subkeys);
                            MappingPairs.Add(pair);
                            
                        }, icon: "add"),
                        new Command(GlobalHelper.Get("key_169"), obj => { MappingPairs.Remove(obj as MappingPair); }, obj => obj is MappingPair,
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

    public class ETLSpliter
    {
        //public List<IColumnProcess> GetSplit(IList<IColumnProcess> etls, IColumnProcess splitPos,bool isBefore,out List<IColumnProcess>subETL)
        //{
        //    subETL = null;
        //    var pos = etls.IndexOf(splitPos);
        //    if(pos==-1)
        //        throw new  ArgumentException("module not exists");
        //    if (isBefore)
        //    {
        //        subETL = etls.Take(pos).ToList();
                 
        //    }
           
            
        //} 
    }


    public class ETLBase : ToolBase, INotifyPropertyChanged
    {
        protected readonly IProcessManager processManager;

        public ETLBase()
        {
            processManager = MainDescription.MainFrm.PluginDictionary["DataProcessManager"] as IProcessManager;
            ETLSelector = new TextEditSelector {GetItems = this.GetAllETLNames()};
            ETLRange = "";
            Column = "column";
            Enabled = true;
            MappingSet = "";
        }

        [Browsable(false)]
        public override string KeyConfig =>ETLSelector?.SelectItem;
        [LocalizedCategory("key_409")]
        [PropertyOrder(1)]
        [LocalizedDisplayName("key_410")]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_240"), obj => SetConfig(), obj => !string.IsNullOrEmpty(ETLSelector.SelectItem),
                            "refresh")
                    }
                    );
            }
        }

        [LocalizedCategory("key_409")]
        [LocalizedDisplayName("key_411")]
        [PropertyOrder(0)]
        [LocalizedDescription("key_412")]
        public TextEditSelector ETLSelector { get; set; }

        [LocalizedCategory("key_409")]
        [LocalizedDisplayName("key_413")]
        [PropertyOrder(2)]
        [LocalizedDescription("key_414")]
        public string ETLRange { get; set; }

        [LocalizedCategory("key_409")]
        [LocalizedDisplayName("key_415")]
        [PropertyOrder(3)]
        [LocalizedDescription("key_416")]
        public string MappingSet { get; set; }
     

        protected SmartETLTool etl { get; set; }

        private void SetConfig()
        {
            Init(null);
            var subTaskModel = new SubTaskModel(Father, etl, this,this.Father.Documents.GetKeys().ToArray(),etl.Documents.GetKeys().ToArray());
            var view = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("key_417")) as UserControl;
            view.DataContext = subTaskModel;

            var name = GlobalHelper.Get("key_418");
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
            etl = this.GetTask<SmartETLTool>(ETLSelector.SelectItem);
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
                    XLogSys.Print.Error(GlobalHelper.Get("key_419") + ex.Message);
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

    [XFrmWork("EtlGE", "EtlGE_desc")]
    public class EtlGE : ETLBase, IColumnGenerator
    {
        [LocalizedDisplayName("gene_mode")]
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

            return process.Generate( IsExecute, documents, this.Father.Analyzer);
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


    [XFrmWork("EtlEX", "EtlEX_desc")]
    public class EtlEX : ETLBase, IDataExecutor
    {
        private EnumerableFunc func;

        [LocalizedDisplayName("key_425")]
        [LocalizedDescription("key_426")]
        public bool AddTask { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);
            var process = GetProcesses().ToList();
            func = process.Aggregate(isexecute: true,analyzer: this.Father.Analyzer); //TODO: BUG
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
                        var task = TemporaryTask<FreeDocument>.AddTempTaskSimple("ETL" + name, func(new List<IFreeDocument> {doc}),
                            d => d.LastOrDefault());
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

    [XFrmWork("DictTF", "DictTF_desc","transform_rotate_right")]
    public class DictTF : TransformerBase
    {
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer)
        {
            if (string.IsNullOrEmpty(Column))
            {
                foreach (var data in datas)
                {
                    yield return data;
                }
                yield break;
            }
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

    [XFrmWork("EtlTF", "EtlTF_desc")]
    public class EtlTF : ETLBase, IColumnDataTransformer
    {
        private EnumerableFunc func;
        private IEnumerable<IColumnProcess> process;

        public EtlTF()
        {
            NewColumn = "";
            IsCycle = false;
        }

        [LocalizedDisplayName("key_431")]
        public bool IsCycle { get; set; }

        [LocalizedDisplayName("key_188")]
        [LocalizedDescription("etl_script_mode")]
        public ScriptWorkMode IsManyData { get; set; }

        [LocalizedCategory("key_211")]
        [LocalizedDisplayName("key_433")]
        [LocalizedDescription("key_434")]
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

        public IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer)
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
                            process.Generate( IsExecute, new List<IFreeDocument> {newdata.Clone()}, this.Father.Analyzer).FirstOrDefault();
                        if (result == null)
                            break;
                        yield return result.Clone();
                        newdata = result;
                    }
                }
                else
                {
                    var result = process.Generate( IsExecute, new List<IFreeDocument> {doc}, this.Father.Analyzer);
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