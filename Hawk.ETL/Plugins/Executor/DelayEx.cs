using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("延时","在工作流中插入延时，单位为ms")]
    public class DelayTF : TransformerBase
    {
      
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }
        [LocalizedDisplayName("延时值")]
        [LocalizedDescription("单位为毫秒")]
        public string DelayTime { get; set; }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var r = data.Query(DelayTime);
                int result = 100;
                if(int.TryParse(r,out result))
                { 
                    Thread.Sleep(result);
                }

                yield return data;
            }
        }
    }
}