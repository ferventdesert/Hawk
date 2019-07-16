using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Executor;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Plugins.Transformers;
using Hawk.ETL.Process;
using Jint.Parser.Ast;

namespace Hawk.ETL.Interfaces
{
    public enum SortType
    {
        AscendSort,

        DescendSort
    }

    [Interface("IColumnProcess")]
    public interface IColumnProcess : IDictionarySerializable
    {
        #region Properties

        string Column { get; set; }

        void SetExecute(bool value);

        bool Enabled { get; set; }


        IEnumerable<IFreeDocument> CheckDatas(IEnumerable<IFreeDocument> docs);


        SmartETLTool Father { get; set; }
        string Description { get; }
        string TypeName { get; }

        string ObjectID { get; set; }
        XFrmWorkAttribute Attribute { get; }

        string Remark { get; set; }

        #endregion

        #region Public Methods

        void Finish();

        bool Init(IEnumerable<IFreeDocument> datas);

        #endregion
    }

    public interface IColumnAdviser : IColumnProcess
    {
        List<IColumnProcess> ManagedProcess { get; }
    }

    public interface IColumnGenerator : IColumnProcess
    {
        /// <summary>
        ///     声明两个序列的组合模式
        /// </summary>
        MergeType MergeType { get; set; }

        IEnumerable<IFreeDocument> Generate(IFreeDocument document = null);

        /// <summary>
        ///     生成器能生成的文档数量
        /// </summary>
        /// <returns></returns>
        int? GenerateCount();
    }

    public class TaskComparer : IComparer<IDataProcess>
    {
        public int Compare(IDataProcess x, IDataProcess y)
        {
            if (x.GetType() == y.GetType())
            {

                var crawlerx = x as SmartCrawler;
                if (crawlerx != null)
                {
                    if (crawlerx?.ShareCookie.SelectItem == y.Name)
                    {
                        return -1;
                    }
                    return 0;
                }

                var etlx = x as SmartETLTool;
                if (
                    etlx?.CurrentETLTools.OfType<ETLBase>().FirstOrDefault(d => d.ETLSelector.SelectItem == y.Name) !=
                    null)
                    return -1;
                return 0;
            }
            if (x is SmartCrawler)

                return 1;
            return -1;
        }
    }

    public interface IColumnDataSorter : IColumnProcess, IComparer<object>
    {
        SortType SortType { get; set; }
    }

    public class DocumentItem
    {
        [PropertyOrder(0)]
        public string Title { get; set; }
        [PropertyEditor("MarkdownEditor")]
        [PropertyOrder(1)]
        public string Document { get; set; }
    }
 
    public static class ETLHelper
    {
        public static IEnumerable<IFreeDocument> ConcatPlus(this IEnumerable<IFreeDocument> d, EnumerableFunc func1,
            IColumnGenerator ge)
        {
            IFreeDocument last = null;
            foreach (var r in func1(d))
            {
                yield return r;
                last = r;
            }
            foreach (var r in ge.Generate(last))
                yield return
                    r;
        }

        public static IEnumerable<AbstractProcessMethod> GetReference(this IColumnProcess obj, IProcessManager manager)
        {
            var etlbase = obj as ETLBase;
            if (etlbase != null)
            {
                
                yield return manager.GetTask<SmartETLTool>(etlbase.ETLSelector.SelectItem);
            }
            var crawler = obj as ResponseTF;
            if (crawler != null)
            {
                yield return crawler.GetTask<SmartCrawler>(crawler.CrawlerSelector.SelectItem);
            }
        }

        /// <summary>
        ///     find all references
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        public static IEnumerable<AbstractProcessMethod> GetReference(this SmartETLTool etl, IProcessManager manager)
        {
            return etl.CurrentETLTools.SelectMany(d => d.GetReference(manager)).Distinct();
        }

        public static IEnumerable<SmartCrawler> GetReference(this SmartCrawler etl, IProcessManager manager)
        {
            var item = manager.GetTask<SmartCrawler>(etl.ShareCookie.SelectItem);
            if (item != null)
                yield return item;

        }

        public static string GetAllToolMarkdownDoc()
        {
            var sb = new StringBuilder();
            var tools = PluginProvider.GetPluginCollection(typeof (IColumnProcess));
            var groupConverter = new GroupConverter();
            foreach (var toolgroup in tools.GroupBy(d => groupConverter.Convert(d, null, null, null)))
            {
                sb.Append(string.Format("# {0}\n", toolgroup.Key));
                foreach (var tool in toolgroup)
                    sb.Append(GetMarkdownScript(tool.MyType, true));
            }
            return sb.ToString();
        }

        public static string GetTotalMarkdownDoc()
        {
            var doc = Application.Current.TryFindResource("hawk_doc");
            if (doc == null)
                return "";
            var str = doc.ToString();

            return str.Replace("$tools_desc$", GetAllToolMarkdownDoc());
        }

        private static string GetDefaultValue(PropertyInfo propertyInfo,Object instance,out string typeName)
        {
            var defaultValue = propertyInfo.GetValue(instance);
             typeName = propertyInfo.PropertyType.Name;
            if (propertyInfo.PropertyType == typeof(ExtendSelector<string>))
            {
                var selector = defaultValue as ExtendSelector<string>;
                defaultValue = selector?.SelectItem;
                typeName = GlobalHelper.Get("string_option");
            }
            else if (propertyInfo.PropertyType == typeof(TextEditSelector))
            {
                var selector = defaultValue as TextEditSelector;
                defaultValue = selector?.SelectItem;
                typeName = GlobalHelper.Get("edit_string_option");
            }
            if (defaultValue == null)
                return "";
            if (defaultValue is Enum)
                return GlobalHelper.GetEnum(defaultValue as Enum);

            return defaultValue.ToString();

        }

        private  static  int GetOrder(PropertyInfo info)
        {
            var customAttributes =
                  (PropertyOrderAttribute[])info.GetCustomAttributes(
                      typeof(PropertyOrderAttribute), false);
            if (!customAttributes.Any())
                return 0;
            return customAttributes.First().Order;
        } 
        public static IEnumerable<CustomPropertyInfo> GetToolProperty(Type tool, Object instance =null,bool mustBrowsable=true)
        {
            Object newinstance;
            if (instance == null)
            {
                instance = PluginProvider.GetObjectInstance(tool) ;
                newinstance = instance;
            }
            else
            {
                newinstance= PluginProvider.GetObjectInstance(tool) ;
                
            }
            var propertys =
              tool.GetProperties().Where(
                  d => d.CanRead && d.CanWrite && AttributeHelper.IsEditableType(d.PropertyType)).ToArray();

            foreach (var propertyInfo in propertys.OrderBy(GetOrder))
            {
                var name = propertyInfo.Name;
                if( name == "ObjectID" || name == "Enabled" || name == "ColumnSelector")
                    continue;
              
                var property = new CustomPropertyInfo();
             
                string typeName = null;
                var defaultValue=GetDefaultValue(propertyInfo,newinstance,out typeName);
                var currentValue=GetDefaultValue(propertyInfo,instance,out typeName);
                property.CurrentValue = currentValue;
                property.DefaultValue = defaultValue;               
                var desc = GlobalHelper.Get("no_desc");
                // var fi =type.GetField(propertyInfo.Name);
                var browseable =
                    (BrowsableAttribute[])propertyInfo.GetCustomAttributes(typeof(BrowsableAttribute), false);
                if (browseable.Length > 0 && browseable[0].Browsable == false&&mustBrowsable)
                    continue;
                var descriptionAttributes =
                    (LocalizedDescriptionAttribute[])propertyInfo.GetCustomAttributes(
                        typeof(LocalizedDescriptionAttribute), false);
                var nameAttributes =
                    (LocalizedDisplayNameAttribute[])propertyInfo.GetCustomAttributes(
                        typeof(LocalizedDisplayNameAttribute), false);
                if (nameAttributes.Length > 0)
                    name = GlobalHelper.Get(nameAttributes[0].DisplayName);
                if (descriptionAttributes.Length > 0)
                    desc = GlobalHelper.Get(descriptionAttributes[0].Description);
                desc = string.Join("\n", desc.Split('\n').Select(d => d.Trim('\t', ' ')));
                property.Desc = desc;
                property.Name = name;
                property.DefaultValue = defaultValue;
                property.OriginName = propertyInfo.Name;
                property.TypeName = typeName;
                yield return  property;
            }
        }

        public class CustomPropertyInfo
        {
            public string Name { get; set; }
            public string Desc { get; set; }
            public string DefaultValue { get; set; }
            public string OriginName { get; set; }
            public string TypeName { get; set; }
            public string CurrentValue { get; set; }
            public override string ToString()
            {
                var defaultValue = DefaultValue;
                var typeName = TypeName;
                if (defaultValue != null && string.IsNullOrWhiteSpace(defaultValue.ToString()) == false)
                    defaultValue = string.Format("{1}:{0}  ", defaultValue, GlobalHelper.Get("default"));
                else
                    defaultValue = "";
                typeName = string.Format("{0}:{1} ", GlobalHelper.Get("key_12"), typeName);


                return string.Format("### {0}({3}):\n* {4}{2}\n* {1}\n", Name, Desc, defaultValue, OriginName,
                    typeName);
            }
        }
        public static string GetMarkdownScript(Type tool, bool isHeader = false)
        {
            var tooldesc = "";

            var attribute = AttributeHelper.GetCustomAttribute(tool);
            if (attribute != null)
                tooldesc = GlobalHelper.Get(attribute.Description);
          

            var sb = new StringBuilder();
            if (isHeader)
            {
                var toolName = GlobalHelper.Get(attribute.Name);
                sb.Append(string.Format("## {0}({1})\n", toolName, tool.Name));
            }

            sb.Append(string.Format("{0}\n", tooldesc));
            foreach (var propertyInfo in GetToolProperty(tool))
            {
                sb.Append(propertyInfo.ToString());
            }
            sb.Append("***\n");
            return sb.ToString();
        }

        public static string GenerateRemark(this IColumnProcess etl, bool addNew,IProcessManager manager)
        {
            var list = new List<string>();
            if (addNew)
                list.Add(GlobalHelper.FormatArgs("drag_desc", etl.TypeName, etl.Column));
            var refs = etl.GetReference(manager).ToList();
            if (refs.Any())
            {
                list.Add(GlobalHelper.FormatArgs("set_before",
                    ",".Join(refs.Where(d=>d!=null).Select(d => string.Format("{0}:{1}", d.TypeName, d.Name)))));
            }
           
         
            list.Add(GenerateItemRemark(etl,new List<string>() { "ObjectID", "Enabled","Column","Remark","Group","Type" , "OneOutput", "IsMultiYield", "IsDebugFilter" }));
            var attribute = AttributeHelper.GetCustomAttribute(etl.GetType());
            if (attribute != null)
            {
                var tooldesc = GlobalHelper.Get(attribute.Description);
                list.Add(GlobalHelper.Get(tooldesc.Split('\n').FirstOrDefault(d => string.IsNullOrWhiteSpace(d) == false)));
            }

            if (!string.IsNullOrWhiteSpace(etl.Remark))
            {
                list.Add(GlobalHelper.RandomFormatArgs("reason_desc", etl.Remark));
            }
            return ",".Join(list.Where(d=>string.IsNullOrWhiteSpace(d)==false));
        }

        public static string GenerateRemark(this SmartETLTool tool, bool addnew,IProcessManager manager)
        {
             var list=new List<string>();
          
            if (addnew)
            {
                list.Add(GlobalHelper.FormatArgs("doc_task_new",tool.TypeName,tool.Name));
            }
            if(string.IsNullOrWhiteSpace(tool.Remark)==false)
            {
                list.Add(GlobalHelper.RandomFormatArgs("reason_desc", tool.Remark));
            }
            list.Add(GenerateItemRemark(tool, new List<string>() { "Name" }));
            int index =1;
            list.Add("\n");
            foreach (var item in tool.CurrentETLTools)
            {
                list.Add(String.Format("{0}. {1}",index, GenerateRemark(item, true, manager)));
                index++;
            }
            return "\n".Join(list);
        }

        private static string GenerateItemRemark( object dict,List<string> ignorekeys=null,bool mustBrowsable=true)
        {
            
            var list=new List<string>();
            foreach (var property in GetToolProperty(dict.GetType(),dict,mustBrowsable))
            {
                var name = property.Name;
                if(property.DefaultValue==property.CurrentValue)
                    continue;
                
                var value = property.CurrentValue;
                if(ignorekeys!=null&&ignorekeys.Contains(property.OriginName))
                    continue;
                if(string.IsNullOrEmpty(value))
                    continue;
                list.Add(GlobalHelper.RandomFormatArgs("set_param_desc", name, value));
            }
            return ",".Join(list.Where(s => string.IsNullOrEmpty(s)==false));
        }

        public static string GenerateRemark(this SmartCrawler tool, bool addnew, IProcessManager manager)
        {
            var list = new List<string>();

            if (addnew)
            {
                list.Add(GlobalHelper.FormatArgs("doc_task_new", tool.TypeName, tool.Name));
            }
            if (string.IsNullOrWhiteSpace(tool.Remark) == false)
            {
                list.Add(GlobalHelper.RandomFormatArgs("reason_desc", tool.Remark));
            }
            list.Add(GenerateItemRemark(tool,new List<string>() {"Remark" , "MainPluginLocation","URL" },false));
                int index =1;
            list.Add("\n");
            foreach (var crawlItem in tool.CrawlItems)
            {
                list.Add(GlobalHelper.FormatArgs("doc_crawler_add_xpath", crawlItem.Name, crawlItem.Format, crawlItem.CrawlType,
                    crawlItem.XPath,index++));
            }
            return "\n\n".Join(list);
        }

        public static string GenerateRemarkPlus(this SmartETLTool tool, bool addnew, IProcessManager manager)
        {
           var refs = tool.GetReference(manager).ToList();
           refs.Add(tool);
           return manager.GenerateRemark(refs);
        }
        public static string GenerateRemark(this  IProcessManager manager, IEnumerable<IDataProcess> collection=null)
        {
            if (collection == null)
                collection = manager.CurrentProcessCollections;
                var list=new List<string>();

            foreach (var  task in collection.OrderByDescending(d => d, new TaskComparer())) 
            {
                list.Add(GlobalHelper.FormatArgs("doc_task_title", task.TypeName, task.Name));
                var etl = task as SmartETLTool;
                if (etl != null)
                {
                    list.Add(GenerateRemark(etl, true, manager));
                }
                var crawler = task as SmartCrawler;
                if (crawler != null)
                {
                    list.Add(GenerateRemark(crawler, true, manager));
                }
            }
            return "\n\n".Join(list);
        }


        public static IList<IColumnProcess> AddModule(this IList<IColumnProcess> etls,
            Predicate<IColumnProcess> condition,
            Func<IColumnProcess, IColumnProcess> addItem, bool isFront)
        {
            etls = etls.ToList();
            var pos = 0;
            while (pos < etls.Count)
            {
                var current = etls[pos];
                if (condition(current))
                {
                    var newetl = addItem(current);
                    if (isFront)
                    {
                        etls.Insert(pos, newetl);
                    }
                    else
                    {
                        if (pos + 1 < etls.Count)
                            etls.Insert(pos + 1, newetl);
                        else
                            etls.Add(newetl);
                    }
                    pos++;
                }
                pos++;
            }
            return etls;
        }

        public static int GetParallelPoint(this IList<IColumnProcess> etls, bool isLastBetter, out ToListTF plTF)

        {
            IEnumerable<IColumnProcess> order;
           

            var pl = etls.OfType<ToListTF>().FirstOrDefault();
            if (pl != null)
            {
                plTF = pl;
                return etls.IndexOf(pl);
            }
            var ignoreTf = new List<Type> {typeof (DelayTF), typeof (RepeatTF)};
            plTF = null;
            var pos=new List<int>();
            foreach (var etl in etls)
            {
                var index = etls.IndexOf(etl);
                var generator = etl as IColumnGenerator;
                if (generator != null)
                {
                    if ((generator.GenerateCount() >1 && index == 0) ||
                        (generator.MergeType == MergeType.Cross && index > 0))
                        pos.Add(index);
                }
                var trans = etl as IColumnDataTransformer;
                if (trans != null && trans.IsMultiYield && ignoreTf.Contains(trans.GetType()) == false)
                    pos.Add(index);
                index++;
            }
            if (pos.Count > 1)
            {
                if(isLastBetter)
                    return pos[pos.Count - 2] + 1;
                return pos[0] + 1;
            }
            if(pos.Count==1)
            {
                return pos[0]+1;
            }
            return 1;
        }

        public static IFreeDocument Transform(this IColumnDataTransformer ge,
            IFreeDocument item, AnalyzeItem analyzeItem)
        {
            if (item == null)
                return new FreeDocument();

            var dict = item;

            object res = null;
            try
            {
                if (ge.OneOutput && dict[ge.Column] == null)
                {
                    if (analyzeItem != null)
                        analyzeItem.EmptyInput += 1;
                }
                else
                {
                    res = ge.TransformData(dict);
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
                if (analyzeItem != null)
                {
                    analyzeItem.Error++;
                        analyzeItem.Analyzer.AddErrorLog(item, ex, ge);
                }

                XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_208"), ge.Column, ge.TypeName, res));
            }

            if (ge.OneOutput)
                if (!string.IsNullOrWhiteSpace(ge.NewColumn))
                {
                    if (res != null)
                        dict.SetValue(ge.NewColumn, res);
                }
                else
                {
                    if (res != null)
                        dict.SetValue(ge.Column, res);
                }


            return dict;
        }

        public static EnumerableFunc FuncAdd(this IColumnProcess tool, EnumerableFunc func, bool isexecute,
            Analyzer analyzer)
        {
            AnalyzeItem analyzeItem = null;
            analyzeItem = analyzer?.Set(tool);
            try
            {
                tool.SetExecute(isexecute);
                if (analyzeItem != null) analyzeItem.HasInit = tool.Init(new List<IFreeDocument>());
            }
            catch (Exception ex)
            {
                if (analyzeItem != null) analyzeItem.HasInit = false;
                XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_209"), tool.Column, tool.TypeName, ex.Message));
                return func;
            }
            if (!tool.Enabled)
                return func;
            if (tool is IColumnDataTransformer)
            {
                var ge = tool as IColumnDataTransformer;
                var func1 = func;
                func = source =>
                {
                    var source2 = func1(source).CountInput(analyzeItem);
                    if (ge.IsMultiYield)
                        return ge.TransformManyData(source2, analyzeItem).CountOutput(analyzeItem);
                    ;
                    return source2.Select(input =>
                    {
                        var now = DateTime.Now;

                        var result = Transform(ge, input, analyzeItem);
                        if (analyzeItem != null)
                            analyzeItem.RunningTime = DateTime.Now - now;
                        return result;
                    }).CountOutput(analyzeItem);
                };
            }

            if (tool is IColumnGenerator)
            {
                var ge = tool as IColumnGenerator;

                var func1 = func;
                switch (ge.MergeType)
                {
                    case MergeType.Append:

                        func = source => source.CountInput(analyzeItem).ConcatPlus(func1, ge).CountOutput(analyzeItem);
                        break;
                    case MergeType.Cross:
                        func = source =>
                            func1(source.CountInput(analyzeItem)).Cross(ge.Generate).CountOutput(analyzeItem);
                        break;

                    case MergeType.Merge:
                        func = source =>
                            func1(source.CountInput(analyzeItem)).MergeAll(ge.Generate()).CountOutput(analyzeItem);
                        break;
                    case MergeType.Mix:
                        func = source =>
                            func1(source.CountInput(analyzeItem)).Mix(ge.Generate()).CountOutput(analyzeItem);
                        break;
                }
            }


            if (tool is IDataExecutor && isexecute)
            {
                var ge = tool as IDataExecutor;
                var func1 = func;
                func = source => ge.Execute(func1(source.CountInput(analyzeItem))).CountOutput(analyzeItem);
            }
            else if (tool is IColumnDataFilter)
            {
                var t = tool as IColumnDataFilter;

                if (t.TypeName == GlobalHelper.Get("key_210"))
                {
                    dynamic range = t;
                    var func1 = func;
                    func = source => func1(source.CountInput(analyzeItem)).Skip((int) range.Skip).Take((int) range.Take)
                        .CountOutput(analyzeItem);
                }
                else

                {
                    var func1 = func;
                    switch (t.FilterWorkMode)
                    {
                        case FilterWorkMode.PassWhenSuccess:
                            func =
                                source =>
                                    func1(source.CountInput(analyzeItem))
                                        .SkipWhile(t.FilteData)
                                        .CountOutput(analyzeItem);
                            break;
                        case FilterWorkMode.ByItem:
                            func =
                                source =>
                                    func1(source.CountInput(analyzeItem)).Where(t.FilteData).CountOutput(analyzeItem);
                            break;
                        case FilterWorkMode.StopWhenFail:
                            func =
                                source =>
                                    func1(source.CountInput(analyzeItem))
                                        .TakeWhile(t.FilteData)
                                        .CountOutput(analyzeItem);
                            break;
                    }
                }
            }
            if (isexecute == false)
            {
                var func1 = func;
                func = source => tool.CheckDatas(func1(source));
            }
            return func;
        }

        public static EnumerableFunc Aggregate(this IEnumerable<IColumnProcess> tools, EnumerableFunc func = null,
            bool isexecute = false, Analyzer analyzer = null)

        {
            if (func == null)
                func = d => d;
            if (analyzer != null)
                analyzer.Items.Clear();
            return tools.Aggregate(func, (current, tool) => FuncAdd(tool, current, isexecute, analyzer));
        }

        public static IEnumerable<IFreeDocument> Generate(this IEnumerable<IColumnProcess> processes, bool isexecute,
            IEnumerable<IFreeDocument> source = null, Analyzer analyzer = null)

        {
            if (source == null)
                source = new List<IFreeDocument>();
            var func = processes.Aggregate(d => d, isexecute, analyzer);
            return func(source);
        }
    }

    public delegate IEnumerable<IFreeDocument> EnumerableFunc(IEnumerable<IFreeDocument> source = null);

    public enum MergeType
    {
        [LocalizedDescription("merge_cross")] Cross,
        [LocalizedDescription("merge_append")] Append,
        [LocalizedDescription("merge_merge")] Merge,
        [LocalizedDescription("merge_mix")] Mix,
        [LocalizedDescription("merge_outputonly")] OutputOnly
    }

    public interface ICacheable
    {
    }

    public class GeneratorBase : ToolBase, IColumnGenerator
    {
        public GeneratorBase()
        {
            Column = TypeName;
            Enabled = true;
            MergeType = MergeType.Cross;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)

        {
            var dict = base.DictSerialize();
            dict.Add("Group", "Generator");
            return dict;
        }

        public virtual IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            yield break;
        }

        [LocalizedCategory("key_211")]
        [LocalizedDisplayName("key_188")]
        public MergeType MergeType { get; set; }

        public virtual int? GenerateCount()
        {
            return null;
        }
    }
}