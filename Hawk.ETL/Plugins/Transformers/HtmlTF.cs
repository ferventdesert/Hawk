using System.ComponentModel;
using System.Web;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    public enum ConvertType
    {
        Decode,
        Encode,
      
    }
    [XFrmWork("URL字符转义","对超链接url生成URL编码后的字符串，用以进行远程访问")]
    public class UrlTF : TransformerBase
    {
        public UrlTF()
        {
        }

        [LocalizedDisplayName("转换选项")]
        public ConvertType ConvertType { get; set; }

        public override object TransformData(IFreeDocument document)
        {
            object item = document[Column];
            if (item == null)
                return "";
            switch (ConvertType)
            {
                  
                case ConvertType.Decode:
                    return HttpUtility.UrlDecode(item.ToString());
                case ConvertType.Encode:
                    return HttpUtility.UrlEncode(item.ToString());
            }
            return "";
        }
    }
    [XFrmWork("HTML字符转义",  "删除HTML标签和转义符号")]
    public class HtmlTF : TransformerBase
    {

        [LocalizedDisplayName("转换选项")]
        public ConvertType ConvertType { get; set; }

        public override object TransformData(IFreeDocument document)
        {
            object item = document[Column];
            if (item == null)
                return "";
            switch (ConvertType)
            {
                case ConvertType.Decode:
                    return HttpUtility.HtmlDecode(item.ToString());
                case ConvertType.Encode:
                    return HttpUtility.HtmlEncode(item.ToString());
            }
            return "";
        }
    }
}