using System;
using System.Collections.Generic;
using Hawk.Core.Utils;
using System.ComponentModel;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Plugins.Transformers;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace Hawk.ETL.Plugins.Web
{
    [XFrmWork("TransTF","TransTF_desc" )]
    public class TransTF : TransformerBase
    {
        private readonly HttpHelper helper;

        public Dictionary<string, string> language = new Dictionary<string, string>
        {
            {GlobalHelper.Get("key_602"), "zh"},
            {GlobalHelper.Get("key_603"), "en"},
            {GlobalHelper.Get("key_604"), "jp"},
            {GlobalHelper.Get("key_605"), "spa"},
            {GlobalHelper.Get("key_606"), "th"},
            {GlobalHelper.Get("key_607"), "ru"},
            {GlobalHelper.Get("key_608"), "yue"},
            {GlobalHelper.Get("key_609"), "de"},
            {GlobalHelper.Get("key_610"), "nl"},
            {GlobalHelper.Get("key_611"), "kor"},
            {GlobalHelper.Get("key_612"), "fra"},
            {GlobalHelper.Get("key_613"), "pt"},
            {GlobalHelper.Get("key_614"), "ara"},
            {GlobalHelper.Get("key_615"), "wyw"},
            {GlobalHelper.Get("key_616"), "auto"},
            {GlobalHelper.Get("key_617"), "it"},
            {GlobalHelper.Get("key_618"), "el"},
        };

        BuffHelper<string> buffHelper=new BuffHelper<string>(50);
        public TransTF()
        {
            Source = new ExtendSelector<string>(language.Keys);
            Target = new ExtendSelector<string>(language.Keys);
            Source.SelectItem = GlobalHelper.Get("key_616");
            Target.SelectItem = GlobalHelper.Get("key_616");
            helper = new HttpHelper();
            ClientID = "";
            Key = "";
            Target.SelectChanged += (s, e) => buffHelper.Clear();
        }

        [LocalizedDisplayName("key_619")]
        public string ClientID { get; set; }

        [LocalizedDisplayName("key")]
        public string Key { get; set; }

        public ExtendSelector<string> Source { get; set; }

        public ExtendSelector<string> Target { get; set; }
        private Random rand = new Random();
        public string Translate(string item)
        {
            var query = buffHelper.Get(item);
            if (query != null)
                return query;
            if (string.IsNullOrWhiteSpace(item))
                return item;
            var httpitem = new HttpItem();
            rand.Next(32768, 65531);
         
            HttpStatusCode code;
            var salt = rand.Next(0, 9999999);
            var md5_str = ClientID + item + salt + Key;
            var sign=EncryptWithMD5(md5_str);
          
            var query_encode=  System.Web.HttpUtility.UrlEncode(item, System.Text.Encoding.UTF8);
            string url =
              $"http://api.fanyi.baidu.com/api/trans/vip/translate?appid={ClientID}&q={query_encode}&from={language[Source.SelectItem]}&to={language[Target.SelectItem]}&salt={salt}&sign={sign}";

            httpitem.URL = url;
            string result = helper.GetHtml(httpitem).Result.Html;
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

        public static string EncryptWithMD5(string source)
        {
            byte[] sor = Encoding.UTF8.GetBytes(source);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(sor);
            StringBuilder strbul = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                strbul.Append(result[i].ToString("x2"));//加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位

            }
            return strbul.ToString();
        }


        public override object TransformData(IFreeDocument datas)
        {
            string item = datas[Column].ToString();
            return Translate(item);
        }
    }
}