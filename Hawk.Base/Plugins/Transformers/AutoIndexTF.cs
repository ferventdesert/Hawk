using System.Collections.Generic;

namespace Hawk.Base.Plugins.Transformers
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