using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Utils
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
            ProxyPort = 18888;
        }

        [PropertyEditor("CodeEditor")]
        [LocalizedCategory("1.请求设置")]
        [LocalizedDisplayName("Header")]
        [PropertyOrder(5)]
        public string Parameters { get; set; }

        [LocalizedCategory("2.代理设置")]
        [LocalizedDisplayName("代理IP")]
        [PropertyOrder(1)]
        public string ProxyIP { get; set; }

        [LocalizedCategory("2.代理设置")]
        [LocalizedDisplayName("端口")]
        [PropertyOrder(2)]
        public int ProxyPort { get; set; }

        [LocalizedCategory("2.代理设置")]
        [LocalizedDisplayName("用户名")]
        [System.Windows.Controls.WpfPropertyGrid.Attributes.PropertyOrder(3)]
        public string ProxyUserName { get; set; }

        [LocalizedCategory("2.代理设置")]
        [LocalizedDisplayName("密码")]
        [PropertyOrder(4)]
        public string ProxyPassword { get; set; }

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
        [LocalizedCategory("1.请求设置")]
        [LocalizedDisplayName("请求方法")]
        [PropertyOrder(1)]
        public MethodType Method { get; set; }

        /// <summary>
        ///     默认请求超时时间
        /// </summary>
        [LocalizedCategory("1.请求设置")]
        [LocalizedDisplayName("超时时间")]
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
        [LocalizedCategory("1.请求设置")]
        [LocalizedDisplayName("请求编码")]
        [LocalizedDescription("当页面出现乱码时，可选择切换UTF-8或GBK")]
        [PropertyOrder(3)]
        public EncodingType Encoding { get; set; }



        /// <summary>
        ///     Post请求时要发送的Post数据
        /// </summary>
        /// 
        [LocalizedCategory("1.请求设置")]
        [LocalizedDisplayName("Post参数")]
        [PropertyOrder(7)]
        [PropertyEditor("CodeEditor")]
        public string Postdata { get; set; }







        /// <summary>
        ///     支持跳转页面，查询结果将是跳转后的页面
        /// </summary>
        /// 
        [LocalizedCategory("1.请求设置")]
        [LocalizedDisplayName("自动重定向")]
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
          
            return dict;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            URL = docu.Set("URL", URL);
            Allowautoredirect = docu.Set("Allowautoredirect", Allowautoredirect);
            Postdata = docu.Set("Postdata", Postdata);
            Encoding = docu.Set("Encoding", Encoding);
            Method = docu.Set("Method", Method);
            Parameters = docu.Set("Parameters", Parameters);
           


        }
    }
}
