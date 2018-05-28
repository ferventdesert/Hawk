using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("空对象过滤器", "检查文本是否为空白符或null，常用")]
    public class NullFT : ToolBase, IColumnDataFilter
    {
        #region Constructors and Destructors

        public NullFT()
        {
            Enabled = true;
            Column = "";
            IsDebugFilter = true;
        }

        #endregion

        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(6)]
        [LocalizedDisplayName("求反")]
        [LocalizedDescription("将结果取反后返回")]
        public bool Revert { get; set; }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize();
            dict.Add("Group", "Filter");

            return dict;
        }

        #region IColumnDataFilter

        public bool FilteData(IFreeDocument data)
        {
            if (IsExecute == false && IsDebugFilter == false)
            {
                return true;
            }
            var r = true;
            r = data != null && FilteDataBase(data);

            return Revert ? !r : r;
        }

        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(8)]
        [LocalizedDisplayName("调试时启用")]
        public bool IsDebugFilter { get; set; }

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