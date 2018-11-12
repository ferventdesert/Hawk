using System;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("RangeFT","RangeFT_desc")]
    public class RangeFT : NullFT
    {
        #region Constants and Fields



        #endregion

        #region Properties

        [LocalizedDisplayName("key_374")]
        public string Max { get; set; }

        [LocalizedDisplayName("key_375")]
        public string Min { get; set; }

        #endregion

        #region Public Methods

        public override string KeyConfig => String.Format("min {0},max {1}", Min,Max);

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