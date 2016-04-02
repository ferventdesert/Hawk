using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("添加新列","添加值为固定值的新列")]
    public class AddNewTF : TransformerBase
    {
        public AddNewTF()
        {
            NewValue = "";
            NewColumn = "NewColumn";
        }

        [DisplayName("生成值")]
        public string NewValue { get; set; }

        public override object TransformData(IFreeDocument free)
        {
            return NewValue;
        }
    }
}