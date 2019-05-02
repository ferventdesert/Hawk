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
    public class RegexTF : SplitBase
    {
        protected Regex regex;
        [Browsable(false)]
        public override string KeyConfig => Script.Substring(Math.Min(100, Script.Length));
        public RegexTF()
        {
            Script = "";
          
        }
       
    

        [PropertyOrder(1)]
        [LocalizedDisplayName("key_380")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }


        public override bool Init(IEnumerable<IFreeDocument> docu)
        {
         
            regex = new Regex(Script);
            return base.Init(docu);
        }

        public override List<string> Split(string str)
        {
            if(string.IsNullOrEmpty(Script))
                return new List<string>() {str};
            MatchCollection r = regex.Matches(str);
            var list = new List<string>();
            for (int i = 0; i < r.Count; i++)
            {
                list.Add(r[i].Value);
            }
            return list;
        }

    }
}