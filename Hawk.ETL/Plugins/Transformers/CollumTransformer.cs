using System;
using System.Collections.Generic;
using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("列名修改器","对列名进行修改" )]
    public class RenameTF : TransformerBase
    {

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            OneOutput = false;
            return base.Init(docus);
        }

        [Browsable(false)]

        public override object TransformData(IFreeDocument document)
        {
            object item = document[Column];

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

    [XFrmWork("删除该列")]
    public class DeleteTF : RenameTF
    {
        public DeleteTF()
        {
        }
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