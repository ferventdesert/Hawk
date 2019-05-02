using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("RegexSplitTF", "RegexSplitTF_desc")]
    public class RegexSplitTF : RegexTF
    {
        [PropertyOrder(3)]
        [LocalizedDisplayName("key_533")]
        [LocalizedDescription("key_534")]
        public bool FromBack { get; set; }

        public override List<string> Split(string str)
        {
            return  regex.Split(str).ToList();
        }

    }

    [XFrmWork("SplitPageTF", "SplitPageTF_desc")]
    public class SplitPageTF : TransformerBase
    {
        public SplitPageTF()
        {
            IsMultiYield = true;
            ItemPerPage = "1";
            MinValue = "1";
        }

        [PropertyOrder(3)]
        [LocalizedDisplayName("key_375")]
        [LocalizedDescription("key_537")]
        public string MinValue { get; set; }

        [PropertyOrder(3)]
        [LocalizedDisplayName("key_538")]
        [LocalizedDescription("key_537")]
        public string ItemPerPage { get; set; }

        protected override IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument document)
        {
            var itemPerPage = 1;
            var total = 1;
            var min = 1;
            var col = string.IsNullOrEmpty(NewColumn) ? Column : NewColumn;
            if (int.TryParse(document.Query(ItemPerPage), out itemPerPage) &&
                int.TryParse(document[Column].ToString(), out total) && int.TryParse(document.Query(MinValue), out min))
            {
                if (itemPerPage == 0)
                    itemPerPage = 1;
                var remainder = total%itemPerPage;

                var totalp = total/itemPerPage;
                if (remainder != 0)
                    totalp += 1;
                for (var i = min; i < min + totalp; i += 1)
                {
                    var doc = document.Clone();
                    doc[col] = i;
                    yield return doc;
                }
            }
        }
    }

    public class SplitBase : TransformerBase
    {
        public SplitBase()
        {
            Index = "0";
            IsManyData = ScriptWorkMode.One;
        }
        [PropertyOrder(0)]
        [LocalizedDisplayName("key_188")]
        [LocalizedDescription("etl_script_mode")]
        public ScriptWorkMode IsManyData { get; set; }
        [LocalizedDisplayName("key_517")]
        [LocalizedDescription("key_544")]
        public string Index { get; set; }



        [LocalizedCategory("key_211")]
        [PropertyOrder(2)]
        [LocalizedDisplayName("key_433")]
        [LocalizedDescription("key_519")]
        public override string NewColumn { get; set; }
        public virtual List<string> Split(string r)
        {
            return new List<string>();
        }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            OneOutput = true;
            IsMultiYield = IsManyData == ScriptWorkMode.List;
            return base.Init(docus);
        }

        public override object TransformData(IFreeDocument datas)
        {
            object result = null;
            //获取输出列
            var o_columns = new List<string>();
            if (string.IsNullOrWhiteSpace(NewColumn))
                o_columns.Add(Column);
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
                var items = Split(r);

                for (var i = 0; i < Math.Min(indexs.Count, o_columns.Count); i++)
                {
                    datas[o_columns[i]] = GetValue(items, indexs[i]);
                }
            }
            return null;
        }


        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer)

        {

            foreach (var data in datas)
    
            {
                object item = data[Column];
                var r = Split(item.ToString());
                foreach (var p in r)
                {
                    var doc = new FreeDocument();
                    doc.MergeQuery(data, NewColumn);
                    doc.SetValue(Column, p);
                    yield return doc.MergeQuery(data, NewColumn);
                }

            }
        }



        protected string GetValue(List<string> arr, int index)
        {
            if (index >= arr.Count)
            {
                return "";
            }
            if (index < 0)
            {
                if (index <= -arr.Count)
                    return "";
                return arr[arr.Count + index];
            }
            return arr[index];
        }
    }

    [XFrmWork("SplitTF", "SplitTF_desc")]
    public class SplitTF : SplitBase
    {
        private List<string> splitstrs;

        public SplitTF()
        {
            SplitChar = "";

        }

        [LocalizedCategory("key_190")]
        [LocalizedDisplayName("key_541")]
        [LocalizedDescription("key_542")]
        public bool ShouldSplitChars { get; set; }

        [LocalizedDisplayName("key_543")]
        public bool SplitPause { get; set; }

        /// <summary>
        ///     此处如果分割空格怎么办？
        /// </summary>
        [LocalizedDisplayName("key_545")]
        [StringEditor("C#")]
        [PropertyEditor("CodeEditor")]
        [LocalizedDescription("key_546")]
        public string SplitChar { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            SplitChar = SplitChar.Replace("\\\\", "\\");
            splitstrs = SplitChar.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (SplitPause || splitstrs.Count == 0)
                splitstrs.Add(" ");
            return base.Init(docus);
        }

        public override List<string> Split(string r)
        {
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
            return items;
        }

        public override object TransformData(IFreeDocument datas)
        {
            object result = null;
            //获取输出列
            var o_columns = new List<string>();
            if (string.IsNullOrWhiteSpace(NewColumn))
                o_columns.Add(Column);
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
                for (var i = 0; i < Math.Min(indexs.Count, o_columns.Count); i++)
                {
                    datas[o_columns[i]] = GetValue(items, indexs[i]);
                }
            }
            return null;
        }
    }
}
