using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("空对象过滤器","检查文本是否为空白符或null")]
    public class NullFT : PropertyChangeNotifier, IColumnDataFilter
    {
        #region Constructors and Destructors

        [Browsable(false)]
        public int ETLIndex { get; set; }
        public NullFT()
        {
            this.Enabled = true;
            this.Column = "";
            IsDebugFilter = true;

        }

        #endregion
        protected bool IsExecute;

        public void SetExecute(bool value)
        {
            IsExecute = value;
        }
        #region Properties

        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(6)]
        [LocalizedDisplayName("求反")]
        [LocalizedDescription("将结果取反后返回")]
        public bool Revert { get; set; }

        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(6)]
        [LocalizedDisplayName("列名")]
        [LocalizedDescription("本模块要处理的列的名称")]
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

        private bool _enabled;

        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(1)]
        [LocalizedDisplayName("启用")]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged("Enabled");
                }


            }
        }



        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("类型")]
        [PropertyOrder(0)]
        public string TypeName
        {
            get
            {
                XFrmWorkAttribute item = AttributeHelper.GetCustomAttribute(this.GetType());
                if (item == null)
                {
                    return this.GetType().ToString();
                }
                return item.Name;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return this.TypeName + " " + this.Column;
        }

        #endregion

        #region Implemented Interfaces

        #region IColumnDataFilter
        public   bool FilteData(IFreeDocument data)
        {
            if (IsExecute == false && IsDebugFilter == false)
            {
                return true;
            }
            bool r = true;
            r = data != null && FilteDataBase(data);
       
            return Revert ? !r : r;
        }
        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(8)]
        [LocalizedDisplayName("调试时启用")]
        public bool IsDebugFilter { get; set; }

        public virtual bool FilteDataBase(IFreeDocument data)

        {
          
            object item = data[this.Column];
            if (item == null)
            {
                return false;
            }
            if (item is string)
            {
                var s = (string)item;
                if (string.IsNullOrWhiteSpace(s))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region IColumnProcess

        public virtual void Finish()
        {
        }

        public virtual bool Init(IEnumerable<IFreeDocument> datas)
        {
            return false;
        }

        #endregion

        #region IDictionarySerializable

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserializePlus(docu);
        }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = this.UnsafeDictSerializePlus();
            dict.Add("Type", this.GetType().Name);
            dict.Remove("ETLIndex");
            dict.Add("Group", "Filter");

            return dict;
        }

        #endregion

        #endregion
    }
}