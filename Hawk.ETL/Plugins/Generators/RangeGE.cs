using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
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
            MaxValue = MinValue = Interval =   "1";
            Column = "id";
        }

        [LocalizedDisplayName("最小值")]
        public string MinValue { get; set; }

        [LocalizedDisplayName("最大值")]
        [LocalizedDescription("除了填写数字，还可以用方括号表达式，如[a]表示从a列获取值作为本参数的真实值")]
        public string MaxValue { get; set; }

        [LocalizedDisplayName("间隔")]
        [LocalizedDescription("如需生成数组1,3,5,7,9，则间隔为2")]
        public string Interval { get; set; }

    

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

        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            int interval;
            double max, min;
            if (int.TryParse(document.Query( Interval), out interval)&& 
                double.TryParse(document.Query(MinValue), out min) && double.TryParse(document.Query(MaxValue), out max))
            {
                for (var i = min; i <= max; i += interval)
                {
                   
                        var item = new FreeDocument();

                        item.Add(Column, Math.Round(i, 5));
                        yield return item;
                }
            }
              
          
        }
    }
}