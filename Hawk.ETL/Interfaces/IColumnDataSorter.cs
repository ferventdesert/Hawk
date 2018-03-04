using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
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

    public enum GenerateMode
    {
        串行模式,
        并行模式
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

    public class GeneratorBase : ToolBase, IColumnGenerator
    {
       

        public GeneratorBase()
        {
            Column = TypeName;
            Enabled = true;
        }
       
        public  override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)

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