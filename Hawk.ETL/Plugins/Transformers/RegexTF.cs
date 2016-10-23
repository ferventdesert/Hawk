using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("正则转换器",  "通过正则表达式提取内容")]
    public class RegexTF : TransformerBase
    {
        protected Regex regex;


        public RegexTF()
        {
            Index = 0;
            Script = "";
        }
        [LocalizedDisplayName("返回多个结果")]
        public override bool IsMultiYield { get; set; }



        [LocalizedDisplayName("匹配编号")]
        [LocalizedDescription("当值为小于0时，可同时匹配多个值")]
        public int Index { get; set; }

        
        [LocalizedDisplayName("表达式")]
        [PropertyEditor("DynamicScriptEditor")]
        public string Script { get; set; }

        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(2)]
        [LocalizedDisplayName("输出列")]
        [LocalizedDescription("若编号为小于0且匹配出多个新列，列名可用可用空格分割，若该列不需要添加，可用_表示，如'_ 匹配1 _'")]
        public override string NewColumn { get; set; }


        public override bool Init(IEnumerable<IFreeDocument> docu)
        {
            OneOutput = true;
            regex = new Regex(Script);
            return base.Init(docu);

        }
        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)

        {
         
            foreach (var data in datas)

            {
                object item = data[Column];
                if (string.IsNullOrEmpty(Script)) 
                    break;

                if (item == null) continue;
                MatchCollection r = regex.Matches(item.ToString());
                foreach (var p in r)
                {
                    var doc=new FreeDocument();
                    doc.Add("regex",p);
                    yield return doc.MergeQuery( data, NewColumn);
                
                }
             
            }
        }

        public override object TransformData(IFreeDocument dict)
        {
            object item = dict[Column];
            if (string.IsNullOrEmpty(Script)) return item;

            if (item == null) return null;
            MatchCollection r = regex.Matches(item.ToString());
            if (Index >= 0)
            {
               
                if (r.Count > Index)
                {
                    return r[Index].Value; ;
                }

            }
            return null;

        }
    }
}