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
    [XFrmWork("UrlTF","UrlTF_desc")]
    public class UrlTF : TransformerBase
    {
        public UrlTF()
        {
        }
        [Browsable(false)]
        public override string KeyConfig => ConvertType.ToString();
        [LocalizedDisplayName("key_485")]
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
    [XFrmWork("HtmlTF",  "HtmlTF_desc")]
    public class HtmlTF : TransformerBase
    {

        [LocalizedDisplayName("key_485")]
        public ConvertType ConvertType { get; set; }

        [Browsable(false)]
        public override string KeyConfig => ConvertType.ToString(); 
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