using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Sorters
{
    public class ColumnDataSorterBase :PropertyChangeNotifier, IColumnDataSorter
    {
        public ColumnDataSorterBase()
        {
            Column = "";
            Enabled = true;
           
        }

        

        public   FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        { 
            var dict = this.UnsafeDictSerialize();
            dict.Add("Type",TypeName);

            dict.Add("Group", "排序");

            return dict;
        }

        protected bool IsExecute; 

        public void SetExecute(bool value)
        {
            IsExecute = value;
        }



        public   void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
           this.UnsafeDictDeserialize(docu);
        }

        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("排序方式")]
        public SortType SortType { get; set; }

        private bool _enabled;


        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("启用")]
        [PropertyOrder(5)]

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


        [Browsable(false)]
        public string Column { get;   set; }

        public virtual int Compare(IFreeDocument a, IFreeDocument b)
        {
            return 0;
        }

        public virtual bool Init(IList<IFreeDocument> datas)
        {
            return false;
        }

        public virtual void Finish()
        {
            
        }

        public bool Init(IEnumerable<IFreeDocument> datas)
        {
            return true;

        }

        [LocalizedDisplayName("介绍")]
        [PropertyOrder(100)]
        public string Description { get; }

        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("类型")]
        [PropertyOrder(0)]
        public string TypeName
        {
            get
            {
                XFrmWorkAttribute item = AttributeHelper.GetCustomAttribute(GetType());
                if (item == null)
                    return GetType().ToString();
                return item.Name;
            }
            
        }
        public override string ToString()
        {

            return this.TypeName + " " + Column;
        }

        public int Compare(object x, object y)
        {
            var a = x as IFreeDocument;
            if (a == null) return 0;
            var b = y as IFreeDocument;
            if (b == null) return 0;
            return Compare(a, b);
        }
    }
}