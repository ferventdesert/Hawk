using System.Net;
using System.Web;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Web
{
    public enum Pattern
    {
        分词,
        词性标注,
        命名实体识别,
        依存句法分析,
        语义依存分析,
        语义角色标注
    }

    [XFrmWork("NlpTF","通过语言云获取的NlpTF功能，包括分词，词性标注，主题提取等")]
    public class NlpTF : TransformerBase
    {
        public NlpTF()
        {
        }

        private readonly string apiKey = "b9D0w08oEOkGTAsF1sxfJK6DOXCQtECRtlSlCqlA";
        private readonly string uriBase = "http://ltpapi.voicecloud.cn/analysis/";
        public ContentType ResultType { get; set; }
        public Pattern Pattern { get; set; }
        private readonly BuffHelper<string> buffHelper = new BuffHelper<string>(50);

        public override object TransformData(IFreeDocument datas)
        {
            var text = datas[Column];
            if (text == null)
                return null;
            var pattern = "all";
            var format = "plain";
            switch (ResultType)
            {
                case ContentType.Json:
                    format = "json";
                    break;
                case ContentType.Text:
                    format = "plain";
                    break;
                case ContentType.XML:
                    format = "xml";
                    break;
                case ContentType.Byte:
                    format = "conll";
                    break;
            }
            switch (Pattern)
            {
                case Pattern.分词:
                    pattern = "ws";
                    break;
                case Pattern.词性标注:
                    pattern = "pos";
                    break;
                case Pattern.依存句法分析:
                    pattern = "dp";
                    break;
                case Pattern.语义依存分析:
                    pattern = "sdp";
                    break;
                case Pattern.语义角色标注:
                    pattern = "srl";
                    break;
                case Pattern.命名实体识别:
                    pattern = "ner";
                    break;
            }

            var param = ("api_key=" + apiKey +
                     
                         "&pattern=" + pattern +
                         "&format=" + format+
                             "&text=" + HttpUtility.UrlEncode(text.ToString())
                         );
            var docs = buffHelper.Get(param);
            if (docs == null)
            {
                var item = new HttpItem();
                item.URL = uriBase+'?'+ param;
                item.Method = MethodType.GET;
                item.Encoding = EncodingType.UTF8;
                var helper = new HttpHelper();
                HttpStatusCode code;
                var response = helper.GetHtml(item).Result;
                if(response.Code== HttpStatusCode.OK)
                    buffHelper.Set(param, response.Html);
                return response;

            }
            return docs;
        }
    }
}