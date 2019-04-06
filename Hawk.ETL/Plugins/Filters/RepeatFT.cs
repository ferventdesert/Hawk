using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("MergeRepeatTF", "MergeRepeatTF_desc","repeat")]
    public class MergeRepeatTF : TransformerBase
    {
        private SortedDictionary<string, IFreeDocument> dictionary;

        public MergeRepeatTF()
        {
            CollectionColumns = "";
            SumColumns = "";
            IsLazyLinq = false;
        }

        [LocalizedDisplayName("key_385")]
        [LocalizedDescription("key_386")]
        public bool IsLazyLinq { get; set; }

        [LocalizedDisplayName("key_387")]
        [LocalizedDescription("key_388")]
        public string CollectionColumns { get; set; }

        [LocalizedDisplayName("key_389")]
        [LocalizedDescription("key_390")]
        public string SumColumns { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            dictionary = new SortedDictionary<string, IFreeDocument>();
            return base.Init(docus);
        }

        //TODO: 此处不能使用枚举式迭代，除非在本模块之后没有其他操作

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer)
        {
            var collColum = CollectionColumns.Split(' ').Select(d => d.Trim()).ToList();
            var sumColum = SumColumns.Split(' ').Select(d => d.Trim()).ToList();

            foreach (var data in datas)
            {
                var item = data[Column];
                if (item == null)
                    continue;
                var key = item.ToString();

                IFreeDocument v;
                if (dictionary.TryGetValue(key, out v))
                {
                    foreach (var r in data)
                    {
                        if (collColum.Contains(r.Key))
                        {
                            var list = v[r.Key] as IList;
                            if (data[r.Key] != null)
                            {
                                if (list != null)
                                {
                                    list.Add(data[r.Key]);
                                }
                                else
                                {
                                    v[r.Key] = new List<object>
                                    {
                                        data[r.Key]
                                    };
                                }
                            }
                        }
                        else if (sumColum.Contains(r.Key))
                        {
                            var vnum = v[r.Key];
                            if (vnum == null)
                                vnum = 0;
                            var v4 = double.Parse(vnum.ToString());
                            var v3 = data[r.Key];
                            if (v3 == null)
                                v3 = 0;
                            var v5 = double.Parse(v3.ToString());
                            v4 += v5;
                            v[r.Key] = v4;
                        }

                        else
                        {
                            if (v[r.Key] == null)
                            {
                                v[r.Key] = r.Value;
                            }
                        }
                    }
                    //yield return v;
                }
                else
                {
                    //显然应当先生成一个新的字典，否则会修改原有集合
                    var newfree = new FreeDocument();
                    data.DictCopyTo(newfree);
                    foreach (var col in collColum)
                    {
                        if (newfree[col] != null)
                            newfree[col] = new List<object> {newfree[col]};
                        else
                        {
                            newfree[col] = new List<object>();
                        }
                    }

                    dictionary.Add(key, newfree);
                    if (IsLazyLinq == false)
                        yield return newfree;
                }
            }
            if (IsLazyLinq)
            {
                foreach (var item in dictionary)
                {
                    yield return item.Value;
                }
            }
        }
    }

    [XFrmWork("RepeatFT", "RepeatFT_desc")]
    public class RepeatFT : NullFT
    {
        private ICollection<string> set;

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            set = new SortedSet<string>();
            return base.Init(datas);
        }

        public override bool FilteDataBase(IFreeDocument data)
        {
            var item = data[Column];
            if (item == null)
                return false;
            var key = item.ToString();
            if (set.Contains(key))
            {
                return false;
            }
            set.Add(key);
            return true;
        }
    }
}