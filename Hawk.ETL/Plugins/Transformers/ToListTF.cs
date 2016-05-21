using System.Collections.Generic;
using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{

    [XFrmWork("启动并行", "该模块在执行时，会将本模块之前的流实例化，转换为实际的list，用于提高并行性能")]
    public class ToListTF : TransformerBase
    {

        public ToListTF()
        {
            DisplayProgress = false;
        }
        [DisplayName("子线程名称")]
        [Description("对每个子线程起的名称")]
        public string IDColumn { get; set; }


        [DisplayName("子线程数量")]
        [Description("每个子线程将要获取的数量，用于显示进度条，可不填")]
        public string MountColumn { get; set; }

        [DisplayName("显示独立任务")]
        [Description("是否将每个子线程插入到任务队列中，从而显示进度")]
        public bool DisplayProgress { get; set; }
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
       
            return base.Init(docus);
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
           
        
            foreach (var data in datas)
            {
              
                yield return data;
            }
        }

    }
}
