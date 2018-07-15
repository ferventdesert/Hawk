using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("AutoIndexTF",  "AutoIndexTF_desc")]
    public class AutoIndexTF : TransformerBase
    {


        private int currindex = 0;
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            currindex = StartIndex;
            return base.Init(docus);
            
        }
         [LocalizedDisplayName("key_472")]
        public int StartIndex { get; set; }
        
        public override object TransformData(IFreeDocument document)
        {

           
            return currindex++;
        }

    }
}