using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("NullFT", "NullFT_desc")]
    public class NullFT : ToolBase, IColumnDataFilter
    {
        [LocalizedCategory("key_211")]
        [PropertyOrder(6)]
        [LocalizedDisplayName("key_366")]
        [LocalizedDescription("key_367")]
        public bool Revert { get; set; }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize();
            dict.Add("Group", "Filter");

            return dict;
        }

        #region Constructors and Destructors

        public NullFT()
        {
            Enabled = true;
            Column = "";
            IsDebugFilter = true;
        }

      

        #endregion

        #region IColumnDataFilter

        public bool FilteData(IFreeDocument data)
        {
            if (IsExecute == false && IsDebugFilter == false)
            {
                return true;
            }
          
            var r = true;
            r = data != null && FilteDataBase(data);
            var value = Revert ? !r : r;
            return value;
        }

        [LocalizedCategory("key_211")]
        [PropertyOrder(8)]
        [LocalizedDisplayName("key_368")]
        public bool IsDebugFilter { get; set; }


        [LocalizedCategory("key_211")]
        [PropertyOrder(8)]
        [LocalizedDisplayName("filter_mode")]
        public FilterWorkMode FilterWorkMode { get; set; }

        public virtual bool FilteDataBase(IFreeDocument data)

        {
            var item = data[Column];
            if (item == null)
            {
                return false;
            }
            if (item is string)
            {
                var s = (string) item;
                if (string.IsNullOrWhiteSpace(s))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region IColumnProcess

        #endregion
    }
}