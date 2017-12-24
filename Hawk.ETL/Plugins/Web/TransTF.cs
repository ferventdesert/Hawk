using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Plugins.Transformers;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace Hawk.ETL.Plugins.Web
{
    [XFrmWork("语言翻译转换","从当前语言翻译为目标语言" )]
    public class TransTF : TransformerBase
    {
        private readonly HttpHelper helper;

        public Dictionary<string, string> language = new Dictionary<string, string>
        {
            {"中文", "zh"},
            {"英语", "en"},
            {"日语", "jp"},
            {"西班牙语", "spa"},
            {"泰语", "th"},
            {"俄罗斯语", "ru"},
            {"粤语", "yue"},
            {"德语", "de"},
            {"荷兰语", "nl"},
            {"韩语", "kor"},
            {"法语", "fra"},
            {"葡萄牙语", "pt"},
            {"阿拉伯语", "ara"},
            {"文言文", "wyw"},
            {"自动检测", "auto"},
            {"意大利语", "it"},
            {"希腊语", "el"},
        };

        BuffHelper<string> buffHelper=new BuffHelper<string>(50);
        public TransTF()
        {
            Source = new ExtendSelector<string>(language.Keys);
            Target = new ExtendSelector<string>(language.Keys);
            Source.SelectItem = "自动检测";
            Target.SelectItem = "自动检测";
            ClientID = "0CupOSsCC4YaDozfkC9gE5EO";
            helper = new HttpHelper();
     
            Target.SelectChanged += (s, e) => buffHelper.Clear();
        }

        [LocalizedDisplayName("应用中心账号")]
        public string ClientID { get; set; }

      

        public ExtendSelector<string> Source { get; set; }

        public ExtendSelector<string> Target { get; set; }

        public string Translate(string item)
        {
            var res = buffHelper.Get(item);
            if (res != null)
                return res;
            if (string.IsNullOrWhiteSpace(item))
                return item;
            var httpitem = new HttpItem();

            string url =
                $"http://openapi.baidu.com/public/2.0/bmt/translate?client_id={ClientID}&q={item}&from={language[Source.SelectItem]}&to={language[Target.SelectItem]}";
            httpitem.URL = url;
            HttpStatusCode code;

            string result = helper.GetHtml(httpitem,out code);
            var r = JsonConvert.Import(result) as JsonObject;

            if (r.Contains("error_code ") == false)
            {
                var sb = new StringBuilder();
                var array = r["trans_result"] as JsonArray;
                for (int i = 0; i < array.Length; i++)
                {
                    var j = array[i] as JsonObject;
                    object r2 = j["dst"];
                    sb.AppendLine(r2.ToString());
                }
                string t = sb.ToString();
                buffHelper.Set(item,t);
                return t;
            }
            return "Error";
        }

      
        

        public override object TransformData(IFreeDocument datas)
        {
            string item = datas[Column].ToString();
            return Translate(item);
        }
    }
}