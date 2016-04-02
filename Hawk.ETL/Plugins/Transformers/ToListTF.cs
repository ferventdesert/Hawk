using System.Collections.Generic;
using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{

    [XFrmWork("列表实例化", "该模块在执行时，会将本模块之前的序列，转换为实际的list，用于提高并行性能")]
    public class ToListTF : TransformerBase
    {

        public ToListTF()
        {
            DisplayProgress = false;
        }
        public string IDColumn { get; set; }

        public string MountColumn { get; set; }

        public bool DisplayProgress { get; set; }
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
        
            return base.Init(docus);
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            return datas;
        }

    }
}
