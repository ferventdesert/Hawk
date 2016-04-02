using System;
using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("清除空白符", "清除字符串前后和中间的空白符")]
    public class TrimTF : TransformerBase
    {
        public TrimTF()
        {
            ReplaceBlank = false;
            ReplaceInnerBlank = true;
        }

        [DisplayName("清除中间空格")]
        public bool ReplaceBlank { get; set; }

        [DisplayName("空白符替换为空格")]
        public bool ReplaceInnerBlank { get; set; }
       


        public override object TransformData(IFreeDocument datas)
        {
            object item = datas[Column];
            if (item == null)
                return "";
            var v = item.ToString();
            v = v.Trim();
            if (ReplaceInnerBlank == true)
            {
                v = v.Replace('\t', ' ');
                v = v.Replace(Environment.NewLine, "");
            }
            if (ReplaceBlank == true)
            {
                v = v.Replace(" ", "");
            }
            return v;

        }
        
    }
}