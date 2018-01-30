using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Plugins.Transformers;
using Hawk.ETL.Plugins.Web;

namespace Hawk.ETL.Plugins.Filters
{
    [XFrmWork("路径是否存在", "判断某一个文件是否已经在指定路径上")]
    public class FileExistFT : TransformerBase
    {
        public override object TransformData(IFreeDocument data)
        {
            var path = data[Column].ToString();
            if (File.Exists(path))
                return "True";
            return "False";
        }

      
    }
    [XFrmWork("正则筛选器","编写正则表达式来过滤文本" )]
    public class RegexFT : NullFT
    {
          protected Regex regex;

 
        public RegexFT()
        {
            Count = 1;
            Script = "";
        }

        
        [LocalizedDisplayName("表达式")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }

        [LocalizedDisplayName("最小匹配数")]
        [LocalizedDescription("只有正则表达式匹配该文本的结果数量大于等于该值时，才会保留，默认为1")]
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