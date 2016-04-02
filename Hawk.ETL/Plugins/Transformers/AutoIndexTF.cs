using System.Collections.Generic;
using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("自增键生成",  "自动生成一个从起始索引开始的自增新列")]
    public class AutoIndexTF : TransformerBase
    {


        private int currindex = 0;
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            currindex = StartIndex;
            return base.Init(docus);
            
        }
         [DisplayName("起始索引")]
        public int StartIndex { get; set; }
        
        public override object TransformData(IFreeDocument document)
        {

           
            return currindex++;
        }

    }
}