using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("正则筛选器","编写正则表达式来过滤文本" )]
    public class RegexFT : NullFT
    {
          protected Regex regex;

 
        public RegexFT()
        {
            Count = 1;
            Script = "";
        }

        
        [DisplayName("表达式")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }

        [DisplayName("最小匹配数")]
        [Description("只有正则匹配的数量大于等于该值时，才会通过")]
        public int Count { get; set; }

      
        public override bool FilteDataBase(IFreeDocument data)
        {
            object item = data[Column];
            if (item == null)
                return true;
            if (string.IsNullOrEmpty(Script)) return true;

            MatchCollection r = regex.Matches(item.ToString());
            if (r.Count < Count)
                return false;
            return true;

        }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
         
            regex = new Regex(Script);
            return base.Init(datas);
        }
    }
}