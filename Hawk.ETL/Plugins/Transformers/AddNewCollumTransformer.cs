using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("AddNewTF","AddNew_desc","list")]
    public class AddNewTF : TransformerBase
    {
        public AddNewTF()
        {
            NewValue = "";
            NewColumn = "NewColumn";
        }

        [LocalizedDisplayName("key_469")]
        public string NewValue { get; set; }

        public override object TransformData(IFreeDocument free)
        {
            return free.Query(NewValue);
        }
    }


}