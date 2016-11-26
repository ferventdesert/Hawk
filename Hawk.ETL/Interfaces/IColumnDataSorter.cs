using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

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

        int ETLIndex { get; set; }
        string Description { get; }
        string TypeName { get; }

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

        IEnumerable<FreeDocument> Generate(IFreeDocument document = null);

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
        Mix
    }

    public class GeneratorBase : PropertyChangeNotifier, IColumnGenerator
    {
        private bool _enabled;
        protected bool IsExecute;

        public GeneratorBase()
        {
            Column = TypeName;
            Enabled = true;
        }
        [Browsable(false)]
        public int ETLIndex { get; set; }
        public void SetExecute(bool value)
        {
            IsExecute = value;
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = this.UnsafeDictSerialize();
            dict.Add("Type", GetType().Name);
            dict.Add("Group", "Generator");
            dict.Remove("ETLIndex");
            return dict;
        }

        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserialize(docu);
        }

        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(1)]
        [LocalizedDisplayName("列名")]
        public string Column { get; set; }

        [LocalizedDisplayName("介绍")]
        [PropertyOrder(100)]
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
        [LocalizedDisplayName("启用")]
        [PropertyOrder(5)]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                OnPropertyChanged("Enabled");
            }
        }

        [Browsable(false)]
        public string TypeName
        {
            get
            {
                var item = AttributeHelper.GetCustomAttribute(GetType());
                return item == null ? GetType().ToString() : item.Name;
            }
        }

        public virtual void Finish()
        {
        }

        public virtual bool Init(IEnumerable<IFreeDocument> datas)
        {
            return true;
        }


        public virtual IEnumerable<FreeDocument> Generate(IFreeDocument document = null)
        {
            yield break;
        }

        [LocalizedDisplayName("生成模式")]
        public MergeType MergeType { get; set; }

        public virtual int? GenerateCount()
        {
            return null;
        }

        public override string ToString()
        {
            return TypeName + " " + Column;
        }
    }
}