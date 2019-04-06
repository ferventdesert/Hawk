using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

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

        [Browsable(false)]
        public override string KeyConfig => NewValue; 
        [LocalizedDisplayName("gene_value")]
        public string NewValue { get; set; }

        public override object TransformData(IFreeDocument free)
        {
            return free.Query(NewValue);
        }
    }


}