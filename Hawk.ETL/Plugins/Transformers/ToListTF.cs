using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{

    [XFrmWork("启动并行", "该模块在执行时，会将本模块之前的流实例化，转换为实际的list，用于提高并行性能")]
    public class ToListTF : TransformerBase
    {

        public ToListTF()
        {
            DisplayProgress = true;
            GroupMount = 1;
        }
        [LocalizedDisplayName("子线程名称")]
        [LocalizedDescription("对每个子线程起的名称")]
        public string IDColumn { get; set; }


        [LocalizedDisplayName("子线程数量")]
        [LocalizedDescription("每个子线程将要获取的数量，用于显示进度条，可不填")]
        public string MountColumn { get; set; }


        [LocalizedDisplayName("分组并行数量")]
        [LocalizedDescription("将多个种子合并为一个任务执行，这对于小型种子任务可有效提升效率")]
        public int GroupMount { get; set; }

        [LocalizedDisplayName("显示独立任务")]
        [LocalizedDescription("是否将每个子线程插入到任务队列中，从而显示进度")]
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
