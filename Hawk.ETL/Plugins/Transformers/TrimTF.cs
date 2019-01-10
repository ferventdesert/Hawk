using System;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("TrimTF", "TrimTF_desc")]
    public class TrimTF : TransformerBase
    {
        public TrimTF()
        {
            ReplaceBlank = false;
            ReplaceInnerBlank = true;
        }

        [LocalizedDisplayName("key_573")]
        public bool ReplaceBlank { get; set; }

        [LocalizedDisplayName("key_574")]
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