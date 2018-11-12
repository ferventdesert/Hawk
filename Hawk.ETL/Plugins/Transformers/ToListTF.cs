using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Managements;

namespace Hawk.ETL.Plugins.Transformers
{

    [XFrmWork("ToListTF", "ToListTF_desc")]
    public class ToListTF : TransformerBase
    {

        public ToListTF()
        {
            DisplayProgress = true;
            GroupMount = 1;
        }
     


        [LocalizedDisplayName("key_560")]
        [LocalizedDescription("key_561")]
        public string MountColumn { get; set; }


        [LocalizedDisplayName("key_562")]
        [LocalizedDescription("key_563")]
        public int GroupMount { get; set; }

        [LocalizedDisplayName("key_564")]
        [LocalizedDescription("key_565")]
        public bool DisplayProgress { get; set; }
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
       
            return base.Init(docus);
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer)
        {
           
        
            foreach (var data in datas)
            {
              
                yield return data;
            }
        }

    }
}
