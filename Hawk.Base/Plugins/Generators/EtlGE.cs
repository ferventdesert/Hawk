using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Hawk.Base.Interfaces;
using Hawk.Base.Managements;
using Hawk.Base.Plugins.Transformers;
using Hawk.Base.Utils.Plugins;
using ExtendEnumerable = Hawk.Base.Utils.ExtendEnumerable;

namespace Hawk.Base.Plugins.Generators
{
   


    public class ETLBase : ToolBase, INotifyPropertyChanged
    {
        protected readonly IProcessManager processManager;
        private string _etlSelector;

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
        [BrowsableAttribute.LocalizedCategoryAttribute("key_409")]
        [BrowsableAttribute.PropertyOrderAttribute(1)]
        [LocalizedDisplayName("key_410")]
      
        [BrowsableAttribute.LocalizedCategoryAttribute("key_409")]
        [LocalizedDisplayName("key_411")]
        [BrowsableAttribute.PropertyOrderAttribute(0)]
        [LocalizedDescription("key_412")]
        public TextEditSelector ETLSelector { get; set; }

        [BrowsableAttribute.LocalizedCategoryAttribute("key_409")]
        [LocalizedDisplayName("key_413")]
        [BrowsableAttribute.PropertyOrderAttribute(2)]
        [LocalizedDescription("key_414")]
        public string ETLRange { get; set; }

        [BrowsableAttribute.LocalizedCategoryAttribute("key_409")]
        [LocalizedDisplayName("key_415")]
        [BrowsableAttribute.PropertyOrderAttribute(3)]
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
            ExtendEnumerable.DictCopyTo(doc, newdoc);
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
        [LocalizedDisplayName("key_422")]
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
            ExtendEnumerable.SetValue(data, "Group", "Generator");
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
            func = process.Aggregate(isexecute: true);
            return true;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var data = base.DictSerialize(scenario);
            ExtendEnumerable.SetValue(data, "Group", "Executor");
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
            var hasyield = false;
            var results = datas.ToList();
            var columns = results.Select(d => d[Column].ToString()).ToList();
            var all_keys = ExtendEnumerable.GetKeys(results, count: 100).ToList();
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

        [BrowsableAttribute.LocalizedCategoryAttribute("key_211")]
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
            ExtendEnumerable.AddRange(data, result);
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
                            process.Generate( IsExecute, new List<IFreeDocument> {ExtendEnumerable.Clone(newdata)}).FirstOrDefault();
                        if (result == null)
                            break;
                        yield return ExtendEnumerable.Clone(result);
                        newdata = result;
                    }
                }
                else
                {
                    var result = process.Generate( IsExecute, new List<IFreeDocument> {doc});
                    foreach (var item in result)
                    {
                        yield return ExtendEnumerable.MergeQuery(item, data, NewColumn);
                    }
                }
            }
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var data = base.DictSerialize(scenario);
            ExtendEnumerable.SetValue(data, "Group", "Transformer");
            return data;
        }
    }
}