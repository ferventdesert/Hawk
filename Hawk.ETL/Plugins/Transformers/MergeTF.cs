using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("合并多列", "将多个列组合成同一列，形如'http:\\{0}:{1},{2}...'输入列的序号为0，之后的1,2分别代表【其他项】的第0和第1个值，常用")]
    public class MergeTF : TransformerBase
    {

        public MergeTF()
        {
            MergeWith = "";
            Format = "";
            ReferFormat = new ExtendSelector<string>();
            ReferFormat.GetItems = () =>
                processManager.CurrentProcessCollections.OfType<SmartCrawler>().Select(d => d.URL).ToList();
            ReferFormat.SelectChanged = (s, e) =>
            {
                if (ReferFormat.SelectItem != "")
                {
                   // Format = ReferFormat.SelectItem;
                    //OnPropertyChanged("Format");
                }
             
            };
        }

        [LocalizedDisplayName("其他项")]
        [LocalizedDescription("写入多个列名，中间使用空格分割，若合并输入列，则可以为空")]
        public string MergeWith { get; set; }

        [PropertyEditor("CodeEditor")]
        [LocalizedDisplayName("格式")]
        [LocalizedDescription("形如'http:\\{0}:{1},{2}...'输入列的序号为0，之后的1,2分别代表【其他项】的第0和第1个值")]
        public string Format { get; set; }

        [LocalizedDisplayName("参考格式")]
        [LocalizedDescription("为了方便用户，下拉菜单中提供了已有网页采集器配置的url，可修改后使用")]
        public ExtendSelector<string> ReferFormat { get; set; }

        public override object TransformData(IFreeDocument datas)
        {
            var item = datas[Column];
            if (item == null)
                item = "";
            var strs = new List<object> {item};
            if (string.IsNullOrEmpty(Format))
                return item;
            var format = datas.Query(Format);
            var columns = MergeWith.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            strs.AddRange(columns.Select(key =>
            {
                if (datas.ContainsKey(key))

                    return datas[key];
                return key;
            }));
            return string.Format(format, strs.ToArray());
        }
    }
}