using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("数值范围过滤器","从数值列中筛选出从最小值到最大值范围的文档")]
    public class RangeFT : NullFT
    {
        #region Constants and Fields



        #endregion

        #region Properties

        [LocalizedDisplayName("最大值")]
        public string Max { get; set; }

        [LocalizedDisplayName("最小值")]
        public string Min { get; set; }

        #endregion

        #region Public Methods

     

        public override bool FilteDataBase(IFreeDocument data)
        {
            object item = data[this.Column];
            if (item == null)
            {
                return false;
            }

            bool res = false;
            var v = (double)AttributeHelper.ConvertTo(item, SimpleDataType.DOUBLE, ref res);
            if (res == false)
            {
                return false;
            }
            double max=1, min=0;
            if (double.TryParse(data.Query(Max), out max) && double.TryParse(data.Query(Min), out min))
                return v >= min && v <=max;
            return true;
        }

        #endregion
    }
}