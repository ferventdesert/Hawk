using System.Collections.Generic;
using System.Threading;
using Hawk.Base.Plugins.Transformers;

namespace Hawk.Base.Plugins.Executor
{
    [XFrmWork("DelayTF", "DelayTF_desc","timer_stop")]
    public class DelayTF : TransformerBase
    {
        [LocalizedDisplayName("key_353")]
        [LocalizedDescription("key_354")]
        public string DelayTime { get; set; }

        [Browsable(false)]
        public override string KeyConfig => DelayTime; 
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }

        protected override IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument data)
        {
            var r = data.Query(DelayTime);
            var result = 100;
            if (int.TryParse(r, out result))
            {
                Thread.Sleep(result);
            }
            return new List<IFreeDocument>() {data};
        }
    }
}