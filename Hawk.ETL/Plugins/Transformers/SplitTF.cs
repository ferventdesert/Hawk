using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("正则分割", "使用正则表达式分割字符串")]
    public class RegexSplitTF : RegexTF
    {
        [DisplayName("倒序")]
        [Description("勾选此项后，选择从后数的第n项")]
        public bool FromBack { get; set; }

        public override object TransformData(IFreeDocument dict)
        {
            var item = dict[Column];
            if (item == null)
                return null;

            var items = regex.Split(item.ToString());

            if (items.Length <= Index)
                return null;
            if (FromBack == false)
                dict.SetValue(NewColumn, items[Index]);
            else
            {
                var index = items.Length - Index - 1;
                if (index < 0)
                    return null;
                dict.SetValue(NewColumn, items[index]);
            }


            return null;
        }
    }

    //[XFrmWork("编码转换")]
    //public class CodingTransform : TransformerBase
    //{
    //    private Encoding source, target;

    //    public CodingTransform()
    //    {
    //        SourceType = new ExtendSelector<string>(Encoding.GetEncodings().Select(d => d.Name));
    //        TargetType = new ExtendSelector<string>(Encoding.GetEncodings().Select(d => d.Name));
    //    }

    //    [DisplayName("源编码")]
    //    public ExtendSelector<string> SourceType { get; set; }

    //    [DisplayName("目标编码")]
    //    public ExtendSelector<string> TargetType { get; set; }

    //    public override bool Init(IEnumerable<IFreeDocument> docus)
    //    {
    //        source = Encoding.GetEncoding(SourceType.SelectItem);
    //        target = Encoding.GetEncoding(TargetType.SelectItem);
    //        return base.Init(docus);
    //    }

    //    public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
    //    {
    //        base.DictDeserialize(docu, scenario);
    //        SourceType.SelectItem = docu.Set("Source", SourceType.SelectItem);
    //        TargetType.SelectItem = docu.Set("Target", TargetType.SelectItem);
    //    }

    //    public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
    //    {
    //        var dict = base.DictSerialize(scenario);
    //        dict.Add("Source", SourceType.SelectItem);
    //        dict.Add("Target", TargetType.SelectItem);
    //        return dict;
    //    }

    //    public override object TransformData(IFreeDocument datas)
    //    {
    //        var sourcebytes = source.GetBytes(datas[Column].ToString());

    //        var targetbytes = Encoding.Convert(source, target, sourcebytes);

    //        return target.GetString(targetbytes);
    //    }

        [XFrmWork("字符串分割", "通过字符分割字符串")]
        public class SplitTF : TransformerBase
        {
            private List<string> splitstrs;

            public SplitTF()
            {
                SplitChar = "";
                SplitNull = true;
            }

            [DisplayName("倒序")]
            public bool FromBack { get; set; }

            [DisplayName("分割字符")]
            public bool ShouldSplitChars { get; set; }

            [DisplayName("空格分割")]
            public bool SplitPause { get; set; }

            [DisplayName("换行分割")]
            public bool SplitNull { get; set; }

            [DisplayName("匹配编号")]
            public int Index { get; set; }

            /// <summary>
            ///     此处如果分割空格怎么办？
            /// </summary>
            [DisplayName("分割字符")]
            [StringEditor("C#")]
            [PropertyEditor("DynamicScriptEditor")]
            [Description("每行一个分割符")]
            public string SplitChar { get; set; }

            public override bool Init(IEnumerable<IFreeDocument> docus)
            {
                splitstrs = SplitChar.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (SplitNull)
                {
                    splitstrs.Add("\n");
                }
                if (SplitPause || splitstrs.Count == 0)
                    splitstrs.Add(" ");
                return base.Init(docus);
            }

            public override object TransformData(IFreeDocument datas)
            {
                object result = null;
                if (datas.ContainsKey(Column))
                {
                    if (datas[Column] == null)
                        return null;
                    var data = datas[Column];


                    var r = data.ToString();

                    List<string> items = null;
                    if (ShouldSplitChars == false)
                    {
                        items = r.Split(splitstrs.ToArray(), StringSplitOptions.RemoveEmptyEntries)
                            .Select(d => d.Trim())
                            .ToList();
                    }
                    else
                    {
                        items = r.Select(d => d.ToString()).ToList();
                    }

                    if (items.Count <= Index)
                        return result;
                 
                    if (FromBack == false)
                        result = items[Index];
                    else
                    {
                        var index = items.Count - Index - 1;
                        if (index < 0)
                            result = items[index];
                    }
                }

                return result;
            }
        }
    
}