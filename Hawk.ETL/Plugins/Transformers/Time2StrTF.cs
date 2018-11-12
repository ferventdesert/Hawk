using System;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("Time2StrTF",  "Time2StrTF_desc")]
    public class Time2StrTF : TransformerBase
    {
        public Time2StrTF()
        {
            Format = "yyyy-MM-dd";
        }

        [LocalizedDisplayName("key_555")]
        public string Format { get; set; }
 
        public override object TransformData(IFreeDocument document)
        {
      
            object item = document[Column];
            DateTime time = (DateTime)item ;
            if (time == null)
                return null;

            return time.ToString(Format);
        }
    }
}