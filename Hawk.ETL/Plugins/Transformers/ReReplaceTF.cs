using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

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
        [PropertyOrder(7)]
        public string ReplaceText { get; set; }

        public override object TransformData(IFreeDocument dict)
        {
            object item = dict[Column];
            var repl = dict.Query(ReplaceText);
            if (item == null)
                return null;

            string r = regex.Replace(item.ToString(), repl);


            return r;
        }
    }
}