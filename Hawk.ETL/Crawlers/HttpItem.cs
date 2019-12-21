using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using EncodingType = Hawk.Core.Utils.EncodingType;

namespace Hawk.ETL.Crawlers
{
    /// <summary>
    ///     Http请求参考类taskListAction
    /// </summary>
    public class HttpItem : IDictionarySerializable
    {
        public HttpItem()
        {
            Timeout = 100000;
            ReadWriteTimeout = 30000;
            Encoding = EncodingType.UTF8;
            Allowautoredirect = true;
            Encoding = EncodingType.Unknown;
            Parameters = "User-Agent: Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.89 Safari/537.36";
        }

        [PropertyEditor("CodeEditor")]
        [LocalizedCategory("request_config")]
        [LocalizedDisplayName("key_116")]
        [PropertyOrder(5)]
        public string Parameters { get; set; }

        [LocalizedCategory("key_117")]
        [LocalizedDisplayName("key_118")]
        [LocalizedDescription("proxy_setting")]
        [PropertyOrder(1)]
        public string ProxyIP { get; set; }


        [LocalizedCategory("key_117")]
        [PropertyOrder(2)]
        public int ProxyPort { get; set; }


        [LocalizedCategory("key_117")]
        [PropertyOrder(3)]
        public string UserName { get; set; }

        [LocalizedCategory("key_117")]
        [PropertyOrder(4)]
        public string Password { get; set; }
        public static bool GetProxyInfo(string proxy,out string url, out string username,out string password)
        {
            username = null;
            password = null;
            if (!proxy.Contains("@"))
            {
                url = proxy;
                return true;
            }
            else
            {
                var pos0 = proxy.IndexOf("@");
                var pos1 = proxy.LastIndexOf("//", 0, pos0);
                var user_pass = proxy.Substring(pos1 + 2, pos0-pos1-2);
                var items = user_pass.Split(':');
                username = items[0];
                if (items.Length > 1)
                    password = items[1];
                url = proxy.Replace(user_pass, "");
                return true;
            }
        }


        public static  string HeaderToString(FreeDocument docu)
        {
            StringBuilder sb=new StringBuilder();
            foreach (var d in docu)
            {
                if(d.Key!="Headers")
                    sb.Append($"{d.Key}:{d.Value}\n");
            }
            sb.Append(docu["Headers"]);
            return sb.ToString();

        }

        public void SetValue(string key, string value)
        {
            var docu = GetHeaderParameter();
            docu.SetValue(key,value);
            this.Parameters = HeaderToString(docu);
        }

        public string GetValue(string key)
        {
            var docu = GetHeaderParameter();
            return docu[key].ToString();
        }
        public FreeDocument GetHeaderParameter()
        {
            var docu = new FreeDocument();
            if (string.IsNullOrEmpty(this.Parameters) == false)
            {
                IEnumerable<string> items = this.Parameters.Split('\n').Select(d => d.Trim());
                var otherheaders = "";
                foreach (string item in items)
                {
                    string[] p = item.Split(':');
                    if (p.Length < 2)
                        continue;
                    string name = p[0].Trim().ToLower();
                    string v = item.Substring(p[0].Length + 1);
                    switch (name)
                    {
                        case "host":
                            docu["Host"] = v;
                            continue;
                        case "user_agent":
                        case "user-agent":
                            docu["User-Agent"] = v;
                            continue;
                        case "accept":
                            docu["Accept"] = v;
                            continue;
                        case "referer":
                            docu["Referer"] = v;
                            continue;
                        case "content-type":
                        case "contenttype":
                        case "content_type":
                            docu["Content_Type"] = v;
                            continue;
                        case "content-length":
                            docu["Content_Length"] = int.Parse(v);
                            continue;
                        case "cookie":
                            docu["Cookie"] = v; 
                            continue;
                    }
                    otherheaders += item+"\n";
                }
                docu["Headers"] = otherheaders;

            }
            return docu;
        }
       
        /// <summary>
        ///     请求URL必须填写
        /// </summary>
        [Browsable(false)]
        public string URL { get; set; }


        /// <summary>
        ///     请求方式默认为GET方式
        /// </summary>
        [LocalizedCategory("request_config")]
        [LocalizedDisplayName("key_120")]
        [PropertyOrder(1)]
        public MethodType Method { get; set; }

        /// <summary>
        ///     默认请求超时时间
        /// </summary>
        [LocalizedCategory("request_config")]
        [LocalizedDisplayName("key_121")]
        [PropertyOrder(3)]
        public int Timeout { get; set; }

        /// <summary>
        ///     默认写入Post数据超时间
        /// </summary>
        [Browsable(false)]
        public int ReadWriteTimeout { get; set; }






        /// <summary>
        ///     返回数据编码默认为NUll,可以自动识别
        /// </summary>
        [LocalizedCategory("request_config")]
        [LocalizedDisplayName("key_122")]
        [LocalizedDescription("key_123")]
        [PropertyOrder(3)]
        public EncodingType Encoding { get; set; }



        /// <summary>
        ///     Post请求时要发送的Post数据
        /// </summary>
        /// 
        [LocalizedCategory("request_config")]
        [LocalizedDisplayName("key_124")]
        [PropertyOrder(7)]
        [PropertyEditor("CodeEditor")]
        public string Postdata { get; set; }







        /// <summary>
        ///     支持跳转页面，查询结果将是跳转后的页面
        /// </summary>
        /// 
        [LocalizedCategory("request_config")]
        [LocalizedDisplayName("key_125")]
        [PropertyOrder(6)]
        public bool Allowautoredirect { get; set; }



        public override string ToString()
        {
            return URL + " " + Method;
        }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("URL", URL);
            dict.Add("Allowautoredirect", Allowautoredirect);
            dict.Add("Postdata", Postdata);
            dict.Add("Encoding", Encoding);
            dict.Add("Method", Method);
            dict.Add("Parameters", Parameters);
            dict.Add("Timeout", Timeout);
            dict.Add("UserName", UserName);
            dict.Add("Password", Password);
            dict.Add("ProxyIP", ProxyIP );
            dict.Add("ProxyPort", ProxyPort );
            return dict;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            URL = docu.Set("URL", URL);
            Allowautoredirect = docu.Set("Allowautoredirect", Allowautoredirect);
            Postdata = docu.Set("Postdata", Postdata);
            Encoding = docu.Set("Encoding", Encoding);
            Method = docu.Set("Method", Method);
            Timeout = docu.Set("Timeout", Timeout);
            UserName = docu.Set("UserName", UserName);
            Password = docu.Set("Password", Password);
            ProxyIP = docu.Set("ProxyIP", ProxyIP);
            Parameters = docu.Set("Parameters", Parameters);
            ProxyPort = docu.Set("ProxyPort", ProxyPort);
           


        }
    }
}
