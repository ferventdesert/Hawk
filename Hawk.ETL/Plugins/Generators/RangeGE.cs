using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Generators
{
    [XFrmWork("RangeGE", "RangeGE_desc")]
    public class RangeGE : GeneratorBase
    {
        public RangeGE()
        {
            Interval = 1.ToString();
            MaxValue = MinValue = Interval = "1";
            Column = "id";
        }

        [LocalizedDisplayName("key_375")]
        public string MinValue { get; set; }

        [LocalizedDisplayName("key_374")]
        [LocalizedDescription("key_457")]
        public string MaxValue { get; set; }

        [LocalizedDisplayName("key_399")]
        [LocalizedDescription("key_458")]
        public string Interval { get; set; }

        [Browsable(false)]
        public override string KeyConfig => string.Format("{0}:{1}:{2}", MinValue, MaxValue, Interval);

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
            if (int.TryParse(document.Query(Interval), out interval) &&
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