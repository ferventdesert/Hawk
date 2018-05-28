using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("列名修改器", "对列名进行修改,常用","page_edit")]
    public class RenameTF : TransformerBase
    {
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            OneOutput = false;
            if (!string.IsNullOrEmpty(NewColumn))
            {
                (Regex.IsMatch(NewColumn, "^\\d+")==false).SafeCheck("列名不能以数字开头，否则将导致无法正确显示该列", LogType.Important);
            }
            return base.Init(docus);
        }

        [Browsable(false)]
        public override object TransformData(IFreeDocument document)
        {
            var item = document[Column];

            if (item != null)
            {
                document.Remove(Column);


                if (!string.IsNullOrEmpty(NewColumn))
                {
                    document.SetValue(NewColumn, item);
                }
                else
                {
                    document.SetValue(Column + "1", item);
                }
            }
            return null;
        }
    }

    [XFrmWork("删除该列","不用解释了吧，常用","delete")]
    public class DeleteTF : RenameTF
    {
        public override object TransformData(IFreeDocument document)
        {
            if (document.ContainsKey(Column))

            {
                document.Remove(Column);
            }
            return null;
        }
    }
}