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
        [PropertyOrder(3)]
        [LocalizedDisplayName("倒序")]
        [LocalizedDescription("勾选此项后，选择从后数的第n项")]
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
                return items[Index];
            else
            {
                var index = items.Length - Index - 1;
                if (index < 0)
                    return null;
                return items[index];
            }


            return null;
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var v = data[Column];
                if (v == null)
                    continue;

                var ps = regex.Split(v.ToString());

            
                foreach (var p in ps)
                {
                    var doc = new FreeDocument();
                  
                    doc.MergeQuery(data, NewColumn);
                    doc.SetValue(Column, p);
                    yield return doc;

                }

            }
           
        }
       
    }

    [XFrmWork("分页", "根据总页数和每页数量进行分页操作，拖入列为总页数，相比于使用Python转换器，可极大地简化操作")]
    public class SplitPageTF : TransformerBase
    {


        public SplitPageTF()
        {
            IsMultiYield = true;
            ItemPerPage = "1";
            MinValue = "1";
        }


        [PropertyOrder(3)]
        [LocalizedDisplayName("最小值")]
        [LocalizedDescription("除了直接填写数值，还可通过方括号表达式从其他列传入")]
        public string MinValue { get; set; }


        [PropertyOrder(3)]
        [LocalizedDisplayName("每页数量")]
        [LocalizedDescription("除了直接填写数值，还可通过方括号表达式从其他列传入")]
        public string ItemPerPage { get; set; }

        protected override IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument document)
        {
            int itemPerPage = 1;
            int total = 1;
            int min = 1;
            var col = string.IsNullOrEmpty(NewColumn) ? Column : NewColumn;
            if (int.TryParse(document.Query(ItemPerPage), out itemPerPage) &&
                int.TryParse(document[Column].ToString(), out total) && int.TryParse(document.Query(MinValue), out min))
            {
                if (itemPerPage == 0)
                    itemPerPage = 1;
                var remainder = total%itemPerPage;
                
                int totalp = total/itemPerPage;
                if (remainder != 0)
                    totalp += 1;
                for (int i = min; i < min+totalp; i += 1)
                {
                    var doc = document.Clone();
                    doc[col] = i;
                    yield return doc;


                }
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

        //    [LocalizedDisplayName("源编码")]
        //    public ExtendSelector<string> SourceType { get; set; }

        //    [LocalizedDisplayName("目标编码")]
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
        //        dict.Set("Source", SourceType.SelectItem);
        //        dict.Set("Target", TargetType.SelectItem);
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
                Index = "0";
                OneOutput = false;
            }

            [LocalizedCategory("高级选项")]
            [LocalizedDisplayName("按字符直接分割")]
            [LocalizedDescription("将原文本每个字符直接分割开")]
            public bool ShouldSplitChars { get; set; }

            [LocalizedDisplayName("空格分割")]
            public bool SplitPause { get; set; }

            [LocalizedDisplayName("匹配编号")]
            [Description("若想获取分割后的第0个元素，则填入0，获取倒数第一个元素，则填入-1 \n可输入多个匹配编号，中间以空格分割，【输出列】也需要与之一对应\n ")]
            public string Index { get; set; }

            /// <summary>
            ///     此处如果分割空格怎么办？
            /// </summary>
            [LocalizedDisplayName("分割字符")]
            [StringEditor("C#")]
            [PropertyEditor("DynamicScriptEditor")]
            [LocalizedDescription("多个分隔符用空格分割，换行符用\\t，制表符用\\t")]
            public string SplitChar { get; set; }

            public override bool Init(IEnumerable<IFreeDocument> docus)
            {
                SplitChar = SplitChar.Replace("\\\\", "\\");
                splitstrs = SplitChar.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (SplitPause || splitstrs.Count == 0)
                    splitstrs.Add(" ");
                return base.Init(docus);
            }

            public override object TransformData(IFreeDocument datas)
            {
                object result = null;
                //获取输出列
                var o_columns = new List<string>();
                if (string.IsNullOrWhiteSpace(this.NewColumn))
                    o_columns.Add(this.Column);
                else
                {
                    o_columns.AddRange(NewColumn.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(d => d.Trim())
                        );
                }
                var indexs = Index.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => int.Parse(d.Trim())).ToList();

                if (datas.ContainsKey(Column))
                {
                    if (datas[Column] == null)
                        return null;
                    var data = datas[Column];
                    var r = data.ToString();
                    List<string> items = null;
                    if (ShouldSplitChars == false)
                    {
                        items = r.Split(splitstrs.ToArray(), StringSplitOptions.None)
                            .Select(d => d.Trim())
                            .ToList();
                    }
                    else
                    {
                        items = r.Select(d => d.ToString()).ToList();
                    }
                    for (int i = 0; i < Math.Min(indexs.Count, o_columns.Count); i++)
                    {
                        datas[o_columns[i]] = GetValue(items, indexs[i]);
                    }
                }
                return null;
            }

            private string GetValue(List<string> arr, int index)
            {
                if (index >= arr.Count)
                {
                    return "";
                }
                else if (index < 0)
                {
                    if (index <= -arr.Count)
                        return "";
                    else
                        return arr[arr.Count + index];
                }
                else
                    return arr[index];

            }
        }

    }
}