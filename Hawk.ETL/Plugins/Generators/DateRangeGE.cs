using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Generators

{
    [XFrmWork("生成区间时间", "生成某范围内的日期和时间","timer_rewind")]
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
        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("最小值")]
        [LocalizedDescription("按类似yyyy-MM-dd HH:mm:ss:ffff格式进行填写")]
        public string MinValue { get; set; }
        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("最大值")]
        [LocalizedDescription("按类似yyyy-MM-dd HH:mm:ss:ffff格式进行填写")]
        public string MaxValue { get; set; }
        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("间隔")]
        [LocalizedDescription("按类似1'h '3'm '5's'格式进行填写")]
        public string Interval { get; set; }
        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("生成时间格式")]
        [LocalizedDescription("可参考C# DateTime Format相关方法， 例如yyyy-MM-dd等")]
        public string Format { get; set; }

        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            //TODO
            DateTime min, max;
            TimeSpan span;
            if (DateTime.TryParseExact(MinValue,
                staticDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out min) && DateTime.TryParseExact(MaxValue,
                    staticDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out max) && TimeSpan.TryParseExact(Interval,
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