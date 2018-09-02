using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using System.Windows.Threading;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Executor;
using Hawk.ETL.Plugins.Transformers;
using Hawk.ETL.Process;
using IronPython.Modules;

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

        SmartETLTool Father { get; set; }
        string Description { get; }
        string TypeName { get; }

        string ObjectID { get; set; }
        XFrmWorkAttribute Attribute { get; }

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

    public interface IColumnDataSorter : IColumnProcess, IComparer<object>
    {
        SortType SortType { get; set; }
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
            {
                yield return
                    r;
            }
        }

        public static string GetAllMarkdownDoc()
        {
            StringBuilder sb=new StringBuilder();
            var tools = PluginProvider.GetPluginCollection(typeof(IColumnProcess));
            var groupConverter= new GroupConverter();
            foreach (var toolgroup in tools.GroupBy(d=>groupConverter.Convert(d,null,null,null)))
            {
                sb.Append(string.Format("# {0}\n", toolgroup.Key));
                foreach (var tool in toolgroup)
                {
                    
                sb.Append(GetMarkdownScript(tool.MyType,true));
                }
            }
            return sb.ToString();
        }
      
        public static string GetMarkdownScript(Type tool, bool isHeader=false)
        {
            string tooldesc = "";
            
            var attribute = AttributeHelper.GetCustomAttribute(tool);
            if (attribute != null)
            {
                tooldesc= GlobalHelper.Get(attribute.Description);
            }
            var instance = PluginProvider.GetObjectInstance(tool) as ToolBase;

            StringBuilder sb = new StringBuilder();
            if (isHeader)
            {
                string toolName = GlobalHelper.Get(attribute.Name);
                sb.Append(string.Format("## {0}({1})\n", toolName,tool.Name));
            }
           
            sb.Append(string.Format("{0}\n",tooldesc));
             var    propertys =
                    tool.GetProperties().Where(
                        d => d.CanRead && d.CanWrite && AttributeHelper.IsEditableType(d.PropertyType)).ToArray();
            foreach (var propertyInfo in propertys)
            {
                string name = propertyInfo.Name;
                if(name== "NewColumn"||name=="ObjectID"||name=="Enabled"|| name=="ColumnSelector")
                  continue;
                var defaultValue = propertyInfo.GetValue(instance);
                var typeName= propertyInfo.PropertyType.Name;
                if (propertyInfo.PropertyType== typeof(ExtendSelector<string>))
                {
                    var selector = defaultValue as ExtendSelector<string>;
                    defaultValue = selector?.SelectItem;
                    typeName = GlobalHelper.Get("string_option");
                }
                if (propertyInfo.PropertyType==typeof(TextEditSelector))
                {
                    var selector = defaultValue as TextEditSelector;
                    defaultValue = selector?.SelectItem;
                    typeName = GlobalHelper.Get("edit_string_option");
                }
                
                string desc = GlobalHelper.Get("no_desc");
               // var fi =type.GetField(propertyInfo.Name);
                var browseable = (BrowsableAttribute[])propertyInfo.GetCustomAttributes(typeof(BrowsableAttribute), false);
                if(browseable.Length>0 && browseable[0].Browsable==false)
                    continue;
                var descriptionAttributes = (LocalizedDescriptionAttribute[])propertyInfo.GetCustomAttributes(typeof(LocalizedDescriptionAttribute), false);
                var nameAttributes = (LocalizedDisplayNameAttribute[])propertyInfo.GetCustomAttributes(typeof(LocalizedDisplayNameAttribute), false);
                if (nameAttributes.Length > 0)
                    name =GlobalHelper.Get( nameAttributes[0].DisplayName);
                if (descriptionAttributes.Length > 0)
                    desc = GlobalHelper.Get(descriptionAttributes[0].Description);
                desc = string.Join("\n", desc.Split('\n').Select(d => d.Trim(new []{'\t',' '}))); 
                if (defaultValue != null && string.IsNullOrWhiteSpace(defaultValue.ToString()) == false)
                    defaultValue = String.Format("{1}:{0}  ",defaultValue,GlobalHelper.Get("default"));
                else
                {
                    defaultValue = "";
                }
                typeName = string.Format("{0}:{1} ",GlobalHelper.Get("key_12"), typeName);
                //string options = "";
                //if (propertyInfo.PropertyType.IsEnum)
                //{
                //    foreach (var e in Enum.GetValues( propertyInfo.PropertyType))
                //    {
                        
                //    }
                //}
                sb.Append(string.Format("### {0}({3}):\n* {4}{2}\n* {1}\n",name,desc,defaultValue,propertyInfo.Name,typeName));

            }
            sb.Append("***\n");
            return sb.ToString();
        }
        public static IList<IColumnProcess> AddModule(this IList<IColumnProcess> etls, Predicate<IColumnProcess> condition,
            Func<IColumnProcess,IColumnProcess> addItem, bool isFront)
        {
            etls = etls.ToList();
            int pos = 0;
            while (pos<etls.Count)
            {
                var current = etls[pos];
                if (condition(current))
                {
                    var newetl = addItem(current);
                    if(isFront)
                        etls.Insert(pos,newetl);
                    else
                    {
                        if(pos+1<etls.Count)
                         etls.Insert(pos+1,newetl);
                        else
                        {
                            etls.Add(newetl);
                        }
                    }
                    pos ++;
                }
                pos++;

            }
            return etls;
        }
        public static int GetParallelPoint(this IList<IColumnProcess> etls,out ToListTF plTF)

        {
            int index = 0;

            var pl = etls.OfType<ToListTF>().FirstOrDefault();
            if (pl != null)
            {
                plTF = pl;
                return etls.IndexOf(pl);
            }
            plTF = null;
            foreach (var etl in etls)
            {
                var generator = etl as IColumnGenerator;
                if (generator != null && generator.GenerateCount() > 0)
                {
                   
                    return index+1;
                }
                var trans = etl as IColumnDataTransformer;
                if (trans != null && trans.IsMultiYield)
                {
                    return index+1;
                }
                index++;
            }

            return 1;

        }

        public static IFreeDocument Transform(this IColumnDataTransformer ge,
            IFreeDocument item,AnalyzeItem analyzeItem)
        {
            if (item == null)
                return new FreeDocument();

            var dict = item;

            object res = null;
            try
            {
                if (ge.OneOutput && dict[ge.Column] == null)
                {
                    if(analyzeItem!=null)
                    analyzeItem.EmptyInput+=1;
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
                analyzeItem.Analyzer.AddErrorLog(item,ex,ge); 
                }
                
                XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_208"), ge.Column,ge.TypeName,res));
            }

            if (ge.OneOutput)
            {
                if (!string.IsNullOrWhiteSpace(ge.NewColumn))
                {
                    if (res != null)
                    {
                        dict.SetValue(ge.NewColumn, res);
                    }
                }
                else
                {
                    dict.SetValue(ge.Column, res);
                }
            }


            return dict;
        }

        public static EnumerableFunc FuncAdd(this IColumnProcess tool, EnumerableFunc func, bool isexecute,Analyzer analyzer)
        {
            AnalyzeItem analyzeItem = null;
                analyzeItem=analyzer?.Set(tool);
            try
            {
                tool.SetExecute(isexecute);
                if (analyzeItem != null) analyzeItem.HasInit = tool.Init(new List<IFreeDocument>());
            }
            catch (Exception ex)
            {
                if (analyzeItem != null) analyzeItem.HasInit = false;
                XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_209"),tool.Column,tool.TypeName,ex));
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
                   var  source2 = func1(source).CountInput(analyzeItem);
                    if (ge.IsMultiYield)
                    {
                        return ge.TransformManyData(source2,analyzeItem).CountOutput(analyzeItem);
                    };
                    return source2.Select(input => Transform(ge, input,analyzeItem)).CountOutput(analyzeItem);
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
                        func = source => func1(source.CountInput(analyzeItem)).Cross(ge.Generate).CountOutput(analyzeItem);
                        break;

                    case MergeType.Merge:
                        func = source => func1(source.CountInput(analyzeItem)).MergeAll(ge.Generate()).CountOutput(analyzeItem);
                        break;
                    case MergeType.Mix:
                        func = source => func1(source.CountInput(analyzeItem)).Mix(ge.Generate()).CountOutput(analyzeItem);
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
                    func = source => func1(source.CountInput(analyzeItem)).Skip((int) range.Skip).Take((int) range.Take).CountOutput(analyzeItem);
                }
                else

                {
                    var func1 = func;
                    func = source => func1(source.CountInput(analyzeItem)).Where(t.FilteData).CountOutput(analyzeItem);
                }
            }
            return func;
        }

     
        public static EnumerableFunc Aggregate(this IEnumerable<IColumnProcess> tools, EnumerableFunc func=null,  bool isexecute=false,Analyzer analyzer=null)

        {
            if (func == null)
                func = d => d;
            if(analyzer!=null)
                analyzer.Items.Clear();
            return tools.Aggregate(func, (current, tool) => FuncAdd(tool, current, isexecute,analyzer));
        }

        public static IEnumerable<IFreeDocument> Generate(this IEnumerable<IColumnProcess> processes, bool isexecute,
            IEnumerable<IFreeDocument> source = null,Analyzer analyzer=null)

        {
            if (source == null)
                source = new List<IFreeDocument>();
            var func = processes.Aggregate(d => d,  isexecute,analyzer);
            return func(source);
        }
    }

    public delegate IEnumerable<IFreeDocument> EnumerableFunc(IEnumerable<IFreeDocument> source = null);

    public enum MergeType
    {
        [LocalizedDescription("merge_append")]
        Append,
        [LocalizedDescription("merge_merge")]
        Merge,
        [LocalizedDescription("merge_cross")]
        Cross,
        [LocalizedDescription("merge_mix")]
        Mix,
        [LocalizedDescription("merge_outputonly")]
        OutputOnly
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
            MergeType= MergeType.Cross;
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