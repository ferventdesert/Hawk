using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("数量范围选择","选择一定数量的行，如跳过前100条，选取50条","filter_alphabetical" )]
    public class NumRangeFT : NullFT
    {
        private int skip;

        private int take;

        private int index = 0;

        [LocalizedDisplayName("跳过")]
        public int Skip
        {
            get { return skip; }
            set
            {
                if (skip != value)
                {
                    skip = value;
                    OnPropertyChanged("Skip");
                }
            }
        }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
           
            this.index = 0;
            return base.Init(datas);
        }

        public override bool FilteDataBase(IFreeDocument data)
        {
            if (index > Skip && index <= Take + Skip)
                return true;
            return false;
        }


        [LocalizedDisplayName("获取")]
        public int Take
        {
            get { return take; }
            set
            {
                if (take != value)
                {
                    take = value;
                    OnPropertyChanged("Take");
                }
            }
        }
 
    }
}