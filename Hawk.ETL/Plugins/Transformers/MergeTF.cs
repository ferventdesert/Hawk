using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("合并多列","将多个列组合成同一列" )]
    public class MergeTF : TransformerBase
    {
        public MergeTF()
        {
            MergeWith = "";
            Format = "";


        }

        [LocalizedDisplayName("其他项")]
        [LocalizedDescription("写入多个列名，中间使用空格分割")]
        public string MergeWith { get; set; }
        [PropertyEditor("CodeEditor")]
        [LocalizedDescription("形如'http:\\{0}:{1},{2}...'本列的序号为0，之后分别为1,2,3..")]
        public string Format { get; set; }
 
        public override object TransformData(IFreeDocument datas)
        {
            object item = datas[Column];
            if (item == null)
                item = "";
            List<object> strs = new List<object>();
            strs.Add(item);
            if (string.IsNullOrEmpty(Format))
                return item;
            var columns = MergeWith.Split(new[]{" "},StringSplitOptions.RemoveEmptyEntries);
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
