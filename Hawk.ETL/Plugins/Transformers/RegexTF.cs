using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Managements;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("RegexTF",  "RegexTF_desc")]
    public class RegexTF : TransformerBase
    {
        protected Regex regex;
        [Browsable(false)]
        public override string KeyConfig => Script.Substring(Math.Min(100, Script.Length));
        public RegexTF()
        {
            Index = 0;
            Script = "";
            IsManyData=ScriptWorkMode.One;
        }
       
        [PropertyOrder(0)]
        [LocalizedDisplayName("key_188")]
        [LocalizedDescription("etl_script_mode")]
        public ScriptWorkMode IsManyData { get; set; }

        [PropertyOrder(2)]
        [LocalizedDisplayName("key_517")]
        [LocalizedDescription("key_518")]
        public int Index { get; set; }

        [PropertyOrder(1)]
        [LocalizedDisplayName("key_380")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }

        [LocalizedCategory("key_211")]
        [PropertyOrder(2)]
        [LocalizedDisplayName("key_433")]
        [LocalizedDescription("key_519")]
        public override string NewColumn { get; set; }


        public override bool Init(IEnumerable<IFreeDocument> docu)
        {
            OneOutput = true;
            IsMultiYield = IsManyData == ScriptWorkMode.List;
            regex = new Regex(Script);
            return base.Init(docu);

        }
        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer)

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
                    doc.MergeQuery(data, NewColumn);
                    doc.SetValue(Column,p);
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