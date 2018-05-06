using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Plugins.Transformers;
using Hawk.ETL.Process;

namespace Hawk.ETL.Interfaces
{
    public enum SortType
    {
        AscendSort,

        DescendSort
    }

    [Interface("ETL模块接口")]
    public interface IColumnProcess : IDictionarySerializable
    {
        #region Properties

        string Column { get; set; }

        void SetExecute(bool value);

        bool Enabled { get; set; }

        SmartETLTool Father { get; set; }
        int ETLIndex { get; set; }
        string Description { get; }
        string TypeName { get; }

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


        public static int GetParallelPoint(this IList<IColumnProcess> etls)

        {
            int index = 0;
          
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
            IFreeDocument item)
        {
            if (item == null)
                return new FreeDocument();

            var dict = item;

            object res = null;
            try
            {
                if (ge.OneOutput && dict[ge.Column] == null)
                {
                }
                else
                {
                    res = ge.TransformData(dict);
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
                XLogSys.Print.Error($"位于{ge.ETLIndex}， 作用在{ge.Column}的模块 {ge.TypeName} 转换出错, 信息{res}");
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

        public static EnumerableFunc FuncAdd(this IColumnProcess tool, EnumerableFunc func, bool isexecute)
        {
            try
            {
                tool.SetExecute(isexecute);
                tool.Init(new List<IFreeDocument>());
            }
            catch (Exception ex)
            {
                XLogSys.Print.Error($"位于{tool.Column}列的{tool.TypeName}模块在初始化时出现异常：{ex},请检查任务参数");
                return func;
            }
            if (!tool.Enabled)
                return func;
            if (tool is IColumnDataTransformer)
            {
                var ge = tool as IColumnDataTransformer;
                var func1 = func;
                func = d =>
                {
                    if (ge.IsMultiYield)
                    {
                        return ge.TransformManyData(func1(d));
                    }
                    var r = func1(d);
                    return r.Select(d2 => Transform(ge, d2));
                };
            }

            if (tool is IColumnGenerator)
            {
                var ge = tool as IColumnGenerator;

                var func1 = func;
                switch (ge.MergeType)
                {
                    case MergeType.Append:

                        func = d => d.ConcatPlus(func1, ge);
                        break;
                    case MergeType.Cross:
                        func = d => func1(d).Cross(ge.Generate);
                        break;

                    case MergeType.Merge:
                        func = d => func1(d).MergeAll(ge.Generate());
                        break;
                    case MergeType.Mix:
                        func = d => func1(d).Mix(ge.Generate());
                        break;
                }
            }


            if (tool is IDataExecutor && isexecute)
            {
                var ge = tool as IDataExecutor;
                var func1 = func;
                func = d => ge.Execute(func1(d));
            }
            else if (tool is IColumnDataFilter)
            {
                var t = tool as IColumnDataFilter;

                if (t.TypeName == "数量范围选择")
                {
                    dynamic range = t;
                    var func1 = func;
                    func = d => func1(d).Skip((int) range.Skip).Take((int) range.Take);
                }
                else

                {
                    var func1 = func;
                    func = d => func1(d).Where(t.FilteData);
                }
            }
            return func;
        }

        public static EnumerableFunc Aggregate(this IEnumerable<IColumnProcess> tools, EnumerableFunc func=null,  bool isexecute=false)

        {
            if (func == null)
                func = d => d;
            return tools.Aggregate(func, (current, tool) => FuncAdd(tool, current, isexecute));
        }

        public static IEnumerable<IFreeDocument> Generate(this IEnumerable<IColumnProcess> processes, bool isexecute,
            IEnumerable<IFreeDocument> source = null)

        {
            if (source == null)
                source = new List<IFreeDocument>();
            var func = processes.Aggregate(d => d,  isexecute);
            return func(source);
        }
    }

    public delegate IEnumerable<IFreeDocument> EnumerableFunc(IEnumerable<IFreeDocument> source = null);

    public enum MergeType
    {
        Append,
        Merge,
        Cross,
        Mix,
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

        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("工作模式")]
        public MergeType MergeType { get; set; }

        public virtual int? GenerateCount()
        {
            return null;
        }
    }
}