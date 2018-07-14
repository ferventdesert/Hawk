using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("延时", "在工作流中插入延时，可休眠固定长度避免爬虫被封禁，单位为ms","timer_stop")]
    public class DelayTF : TransformerBase
    {
        [LocalizedDisplayName("延时值")]
        [LocalizedDescription("单位为毫秒，也可使用方括号语法，例如[a]表示从a列中读取延时长度")]
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