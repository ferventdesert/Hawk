using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;

namespace Hawk.ETL.Plugins.Transformers
{
    public enum RepeatType
    {
        OneRepeat,
        ListRepeat,
    }
    [XFrmWork("RepeatTF", "RepeatTF_desc")]
    public class RepeatTF : TransformerBase
    {
        public RepeatTF()
        {
            RepeatCount = "1";
        }

        [LocalizedDisplayName("repeat_mode")]
        public RepeatType RepeatType { get; set; }

        [LocalizedDisplayName("key_523")]
        public string RepeatCount { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            return base.Init(docus);
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer)
        {
            switch (RepeatType)
            {
                    case RepeatType.ListRepeat:
                    var count = int.Parse(RepeatCount);
                    while (count>0)
                    {
                        foreach (var data in datas)
                        {
                            yield return data.Clone();
                        }
                        count--;
                    }
                    break;
                    case RepeatType.OneRepeat:
                    foreach (var data in datas)
                    {
                        var c = data.Query(RepeatCount);
                        var c2 = int.Parse(c);
                        while (c2 > 0)
                        {
                            yield return data;
                            c2--;
                        }
                    }

                    break;
            } 
        }
    }
}
