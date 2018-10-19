using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Generators

{
    [XFrmWork("DateRangeGE", "DateRangeGE_desc","timer_rewind")]
    internal class DateRangeGE : GeneratorBase
    {
        private readonly string staticDateFormat = "yyyy-MM-dd HH:mm:ss:ffff";
        private readonly string staticSpanFormat = "h'h 'm'm 's's'";

        public DateRangeGE()
        {
            Format = staticDateFormat;
            MaxValue = DateTime.Now.ToString(Format);
            MinValue = (DateTime.Now - TimeSpan.FromDays(3)).ToString(Format);
            Interval = TimeSpan.FromHours(1).ToString(staticSpanFormat);
        }
        [LocalizedCategory("key_21")]
        [LocalizedDisplayName("key_375")]
        [LocalizedDescription("key_398")]
        public string MinValue { get; set; }
        [LocalizedCategory("key_21")]
        [LocalizedDisplayName("key_374")]
        [LocalizedDescription("key_398")]
        public string MaxValue { get; set; }
        [LocalizedCategory("key_21")]
        [LocalizedDisplayName("key_399")]
        [LocalizedDescription("key_400")]
        public string Interval { get; set; }
        [LocalizedCategory("key_21")]
        [LocalizedDisplayName("key_401")]
        [LocalizedDescription("key_402")]
        public string Format { get; set; }


        [Browsable(false)]
        public override string KeyConfig => String.Format("min:{0},max:{1}", MinValue, MaxValue);
        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            //TODO
            DateTime min, max;
            TimeSpan span;
            var minvalue = AppHelper.Query(MinValue, document);
            var maxvalue = AppHelper.Query(MaxValue, document);
            var interval = AppHelper.Query(Interval, document);
            if (DateTime.TryParseExact(minvalue,
                staticDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out min) && DateTime.TryParseExact(maxvalue,
                    staticDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out max) && TimeSpan.TryParseExact(interval,
                        staticSpanFormat,
                        CultureInfo.InvariantCulture,
                        TimeSpanStyles.None,
                        out span))

            {
                for (var i = min; i <= max; i += span)
                {
                    var item = new FreeDocument();
                    item.Add(Column, i.ToString(Format));
                    yield return item;
                }
            }
        }
    }

}