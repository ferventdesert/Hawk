using System.Collections.Generic;
using Hawk.Core.Utils;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("RenameTF", "RenameTF_desc","page_edit")]
    public class RenameTF : TransformerBase
    {
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            OneOutput = false;
            if (!string.IsNullOrEmpty(NewColumn))
            {
                (Regex.IsMatch(NewColumn, "^\\d+")==false).SafeCheck(GlobalHelper.Get("key_475"), LogType.Important);
            }
            return base.Init(docus);
        }
        [Browsable(false)]
        public override string KeyConfig => NewColumn;
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

    [XFrmWork("DeleteTF","DeleteTF_desc","delete")]
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

        [Browsable(false)]
        public override string KeyConfig => Column;
    }
}