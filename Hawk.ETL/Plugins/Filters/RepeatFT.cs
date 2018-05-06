using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("重复项合并", "对重复的数据行，进行合并操作","repeat")]
    public class MergeRepeatTF : TransformerBase
    {
        private SortedDictionary<string, IFreeDocument> dictionary;

        public MergeRepeatTF()
        {
            CollectionColumns = "";
            SumColumns = "";
            IsLazyLinq = false;
        }

        [LocalizedDisplayName("延迟输出")]
        [LocalizedDescription("不勾选此选项使用枚举式迭代，需保证在本模块之后没有其他操作，否则请勾选该选项")]
        public bool IsLazyLinq { get; set; }

        [LocalizedDisplayName("合并到集合的属性")]
        [LocalizedDescription("填入空格分割的列名，对本模块所在列的值相同的所有属性分别进行纵向合并数组")]
        public string CollectionColumns { get; set; }

        [LocalizedDisplayName("求和属性")]
        [LocalizedDescription("填入空格分割的列名，对本模块所在列的值相同的所有属性分别进行纵向求和")]
        public string SumColumns { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = true;
            dictionary = new SortedDictionary<string, IFreeDocument>();
            return base.Init(docus);
        }

        //TODO: 此处不能使用枚举式迭代，除非在本模块之后没有其他操作

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
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

    [XFrmWork("删除重复项", "以拖入的列为唯一主键，自动去重，仅保留重复出现的第一项")]
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