using System;
using System.Collections.Generic;
using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Generators
{
    [XFrmWork("生成区间数","生成某范围内的数值列")]
    public class RangeGE : GeneratorBase
    {
        public RangeGE()
        {
            Interval = 1.ToString();
            RepeatCount = 1.ToString();
            MaxValue = MinValue = Interval = RepeatCount = "1";
            Column = "id";
        }

        [DisplayName("最小值")]
        public string MinValue { get; set; }

        [DisplayName("最大值")]
        public string MaxValue { get; set; }

        [DisplayName("间隔")]
        [Description("如1,3,5,7,9，间隔为2")]
        public string Interval { get; set; }

        [DisplayName("重复次数")]
        [Description("如1,1,2,2,3,3, 重复次数为2")]
        public string RepeatCount { get; set; }

        public override int? GenerateCount()
        {
            int interval;
            double max, min;

            if (int.TryParse(Interval, out interval) && double.TryParse(MaxValue, out max) &&
                double.TryParse(MinValue, out min))
            {
                return (int) ((max - min/interval));
            }
            return -1;
        }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
          

            if (Interval == "0")
                Interval = 1.ToString();
            return base.Init(datas);
        }

        public override IEnumerable<FreeDocument> Generate(IFreeDocument document = null)
        {
            int interval,repeat;
            double max, min;
            if (int.TryParse(document.Query( Interval), out interval)&& int.TryParse(document.Query(RepeatCount), out repeat) &&
                double.TryParse(document.Query(MinValue), out min) && double.TryParse(document.Query(MaxValue), out max))
            {
                for (var i = Position * interval + min; i <= max; i += interval)
                {
                    var j = repeat;
                    while (j > 0)
                    {
                        var item = new FreeDocument();

                        item.Add(Column, Math.Round(i, 5));
                        yield return item;
                        j--;
                    }
                }
            }
              
          
        }
    }
}