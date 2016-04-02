using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("延时")]
  public  class DelayTF:TransformerBase 
    {
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }

        public string DelayTime { get; set; }

        
        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var m = data[Column];
                int time = 0;
                if (m!=null &&int.TryParse(m.ToString(),out time))
                {
                    if(time!=0)
                        Thread.Sleep(time);
                }
            
                yield return data;
            }
        }
    }
}
