using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("DelayTF", "DelayTF_desc","timer_stop")]
    public class DelayTF : TransformerBase
    {
        [LocalizedDisplayName("key_353")]
        [LocalizedDescription("key_354")]
        public string DelayTime { get; set; }

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