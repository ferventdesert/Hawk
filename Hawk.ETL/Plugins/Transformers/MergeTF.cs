using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;
using System.Text.RegularExpressions;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("MergeTF", "MergeTF_desc")]
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

        [Browsable(false)]
        public override string KeyConfig => Format;

        Regex rgx = new Regex(@"\[[^\s\b\]{},!?'""]{1,10}\]|\{[^\s\b\]{},!?'""]{1,10}\}");
        [LocalizedDisplayName("key_502")]
        [LocalizedDescription("key_503")]
        public string MergeWith { get; set; }
       
        [PropertyEditor("CodeEditor")]
        [LocalizedDisplayName("key_504")]
        [LocalizedDescription("MergeTF_format")]
        public string Format { get; set; }

        [LocalizedDisplayName("key_505")]
        [LocalizedDescription("key_506")]
        public ExtendSelector<string> ReferFormat { get; set; }
        public override IEnumerable<string> InputColumns()
        {
            if (!string.IsNullOrEmpty(Column))
                yield return Column;
            if (!string.IsNullOrEmpty(MergeWith))
            {
                foreach (var col in MergeWith.Split(' '))
                {
                    yield return col;
                }
            }

        }
        public override object TransformData(IFreeDocument datas)
        {
            var item = datas[Column];
            if (item == null)
                item = "";
            var strs = new List<object> {item};
            if (string.IsNullOrEmpty(Format))
                return item;
            var format = datas.Query(Format);
            var exps=rgx.Matches(format);
            foreach (Match exp in exps)
            {
                format = format.Replace(exp.Value,datas.Query(exp.Value));
            }
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