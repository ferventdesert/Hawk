using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("正则替换", "通过正则表达式替换数值")]
    public class ReReplaceTF : RegexTF
    {
        public ReReplaceTF()
        {
            ReplaceText = "";

        }

  

        [LocalizedDisplayName("替换为")]
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