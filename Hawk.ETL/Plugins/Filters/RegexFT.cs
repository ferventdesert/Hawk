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
    [XFrmWork("FileExistFT", "FileExistFT_desc")]
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
    [XFrmWork("RegexFT","RegexFT_desc" )]
    public class RegexFT : NullFT
    {
          protected Regex regex;

 
        public RegexFT()
        {
            Count = 1;
            Script = "";
        }

        [Browsable(false)]
        public override string KeyConfig => Script;
        [LocalizedDisplayName("key_380")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }

        [LocalizedDisplayName("key_381")]
        [LocalizedDescription("key_382")]
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