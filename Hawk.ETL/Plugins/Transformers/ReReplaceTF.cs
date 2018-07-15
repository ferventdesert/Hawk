using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("ReReplaceTF", "ReReplaceTF_desc")]
    public class ReReplaceTF : RegexTF
    {
        public ReReplaceTF()
        {
            ReplaceText = "";

        }

  

        [LocalizedDisplayName("key_526")]
        [PropertyEditor("CodeEditor")]
        public string ReplaceText { get; set; }

        public override object TransformData(IFreeDocument dict)
        {
            object item = dict[Column];
            if (item == null)
                return null;

            string r = regex.Replace(item.ToString(), ReplaceText);


            return r;
        }
    }
}