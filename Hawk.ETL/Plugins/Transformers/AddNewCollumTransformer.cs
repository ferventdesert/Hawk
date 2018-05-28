using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("添加新列","为数据集添加新列，值为某固定值","list")]
    public class AddNewTF : TransformerBase
    {
        public AddNewTF()
        {
            NewValue = "";
            NewColumn = "NewColumn";
        }

        [LocalizedDisplayName("生成值")]
        public string NewValue { get; set; }

        public override object TransformData(IFreeDocument free)
        {
            return free.Query(NewValue);
        }
    }


}