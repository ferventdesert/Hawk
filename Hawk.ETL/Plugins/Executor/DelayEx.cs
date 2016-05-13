using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("延时","在工作流中插入延时，单位为ms，值为拖入列的值")]
    public class DelayTF : TransformerBase
    {
      
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var m = data[Column];
                var time = 0;
                if (m != null && int.TryParse(m.ToString(), out time))
                {
                    if (time != 0)
                        Thread.Sleep(time);
                }

                yield return data;
            }
        }
    }
}