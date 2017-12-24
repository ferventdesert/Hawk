using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("合并多列", "将多个列组合成同一列")]
    public class MergeTF : TransformerBase
    {
        private string _format;

        public MergeTF()
        {
            MergeWith = "";
            Format = "";
            ReferFormat = new ExtendSelector<string>();
            ReferFormat.GetItems = () =>
                processManager.CurrentProcessCollections.OfType<SmartCrawler>().Select(d => d.URL).ToList();
            ReferFormat.SelectChanged = (s, e) => Format = ReferFormat.SelectItem;
        }

        [LocalizedDisplayName("其他项")]
        [LocalizedDescription("写入多个列名，中间使用空格分割")]
        public string MergeWith { get; set; }

        [PropertyEditor("CodeEditor")]
        [LocalizedDisplayName("格式")]
        [LocalizedDescription("形如'http:\\{0}:{1},{2}...'本列的序号为0，之后1,2分别为其他项的第0，第1个值")]
        public string Format
        {
            get { return _format; }
            set
            {
                if (_format != value)
                {
                    _format = value;
                    OnPropertyChanged("Format");
                }
            }
        }

        [LocalizedDisplayName("参考格式")]
        public ExtendSelector<string> ReferFormat { get; set; }

        public override object TransformData(IFreeDocument datas)
        {
            var item = datas[Column];
            if (item == null)
                item = "";
            var strs = new List<object> {item};
            if (string.IsNullOrEmpty(Format))
                return item;
            var columns = MergeWith.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            strs.AddRange(columns.Select(key =>
            {
                if (datas.ContainsKey(key))

                    return datas[key];
                return key;
            }));
            return string.Format(Format, strs.ToArray());
        }
    }
}