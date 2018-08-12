using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("key_210","NumRangeFT_desc","filter_alphabetical" )]
    public class NumRangeFT : NullFT
    {
        private int skip;

        private int take;

        private int index = 0;

        [LocalizedDisplayName("key_370")]
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
        [Browsable(false)]
        public override string KeyConfig => String.Format("skip {0},take {1}",Skip,Take);

        public override bool FilteDataBase(IFreeDocument data)
        {
            if (index > Skip && index <= Take + Skip)
                return true;
            return false;
        }


        [LocalizedDisplayName("key_371")]
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