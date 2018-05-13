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
    [XFrmWork("生成随机数", "生成某范围内和指定数量的随机数","share")]
    public class RandomGE : GeneratorBase
    {
        Random random= new Random();

        public RandomGE()
        {
            MaxValue = "100";
            MinValue = "1";
            Count = "100";
            Column = "id";
        }

        [LocalizedDisplayName("最小值")]
        public string MinValue { get; set; }

        [LocalizedDisplayName("最大值")]
        public string MaxValue { get; set; }


        [LocalizedDisplayName("数量")]
        public string Count { get; set; }

        public override int? GenerateCount()
        {

            int count = -1;
            if (int.TryParse(Count,out count))
            {
                return count;
            }
            return count;
        }


        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            int count;
            int max, min;
            if (int.TryParse(document.Query(Count), out count) &&
                int.TryParse(document.Query(MinValue), out min) && int.TryParse(document.Query(MaxValue), out max))
            {
                int i = 0;
                while (i<count)
                {
                    var item = new FreeDocument();

                    item.Add(Column, random.Next(min,max));
                    yield return item;
                    i += 1;
                }
            }


        }
    }
}