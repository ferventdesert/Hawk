using System;
using System.ComponentModel;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("时间转字符串",  "将时间类型转换为特定格式的字符串")]
    public class Time2StrTF : TransformerBase
    {
        public Time2StrTF()
        {
            Format = "yyyy-MM-dd";
        }

        [DisplayName("转换格式")]
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