using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Utils
{
    public enum ContentType
    {
        Text,
        Json,
        XML,
        Byte
    }

    
    public class HttpHelper
    {
        //默认的编码
        private Encoding encoding = Encoding.Default;
        //public bool AutoVisit(HttpItem item)
        //{
        //    var res = GetHtml(item);
        //    XLogSys.Print.Info(res.Substring(0, Math.Min(res.Length, 300)));
        //    var relocate = false;
        //    item.Method = MethodType.GET;
        //    while (true)
        //    {
        //        if (item.ResponseHeaders == null)
        //            break;
        //        var newpos = item.ResponseHeaders["Location"];

        //        if (newpos == null)
        //            break;
        //        XLogSys.Print.Debug("Redirect to " + newpos);
        //        item.URL = newpos;
        //        res = GetHtml(item);
        //        relocate = true;
        //    }
        //    return relocate;
        //}

        private FreeDocument CookieToDict(string cookie)
        {
            var dict = new FreeDocument();
            foreach (var s in cookie.Split(';'))
            {
                // foreach (var s in p.Split(';'))
                {
                    var equalpos = s.IndexOf("=");

                    if (equalpos != -1) //有可能cookie 无=，就直接一个cookiename；比如:a=3;ck;abc=; 
                    {
                        var cookieKey = s.Substring(0, equalpos).Trim();
                        var cookievalue = "";
                        if (equalpos != s.Length - 1) //这种是等号后面无值，如：abc=; 
                        {
                            cookievalue = s.Substring(equalpos + 1, s.Length - equalpos - 1).Trim();
                        }
                        dict.SetValue(cookieKey.Trim(), cookievalue.Trim());
                    }
                    else
                    {
                        var cookieKey = s.Trim();
                        dict.SetValue(cookieKey.Trim(), "");
                    }
                }
            }
            return dict;
        }

        public string MergeCookie(string c1, string c2)
        {
            var dict1 = CookieToDict(c1);
            var dict2 = CookieToDict(c2);
            dict1.DictCopyTo(dict2);

            var v2 = string.Join(";", dict2.Select(d => $"{d.Key}={d.Value}"));
            return v2;
        }

        /// <summary>
        ///     根据相传入的数据，得到相应页面数据
        /// </summary>
        /// <param name="strPostdata">传入的数据Post方式,get方式传NUll或者空字符串都可以</param>
        /// <param name="ContentType">返回的响应数据的类型</param>
        /// <returns>string类型的响应数据</returns>
        private byte[] GetHttpRequestFile(HttpWebRequest request, HttpItem objhttpitem, out HttpStatusCode statusCode)
        {
            byte[] result = null;


            using (var response = (HttpWebResponse) request.GetResponse())
            {
                var _stream = new MemoryStream();

                var docu = objhttpitem.GetHeaderParameter();
                if (response.Headers["set-cookie"] != null)
                    docu["Cookie"] = MergeCookie(docu["Cookie"].ToString(), response.Headers["set-cookie"]);

                statusCode = response.StatusCode;
                objhttpitem.Parameters = HttpItem.HeaderToString(docu);
                //GZIIP处理
                if (response.ContentEncoding != null &&
                    response.ContentEncoding.Equals("gzip", StringComparison.InvariantCultureIgnoreCase))
                {
                    //开始读取流并设置编码方式
                    //new GZipStream(response.GetResponseStream(), CompressionMode.Decompress).CopyTo(_stream, 10240);
                    //.net4.0以下写法
                    _stream =
                        GetMemoryStream(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress));
                }
                else
                {
                    //开始读取流并设置编码方式
                    //response.GetResponseStream().CopyTo(_stream, 10240);
                    //.net4.0以下写法
                    _stream = GetMemoryStream(response.GetResponseStream());
                }
                //获取Byte
                result = _stream.ToArray();
                //是否返回Byte类型数据


                _stream.Close();
            }

            return result;
        }

        /// <summary>
        ///     根据相传入的数据，得到相应页面数据
        /// </summary>
        /// <param name="strPostdata">传入的数据Post方式,get方式传NUll或者空字符串都可以</param>
        /// <param name="ContentType">返回的响应数据的类型</param>
        /// <returns>string类型的响应数据</returns>
        private string GetHttpRequestData(HttpWebRequest request, HttpItem objhttpitem,out WebHeaderCollection responseHeaders, out HttpStatusCode statusCode)
        {
            var result = "";

            #region 得到请求的response

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                MemoryStream stream;

                var docu = objhttpitem.GetHeaderParameter();
                if (response.Headers["set-cookie"] != null)
                    docu["Cookie"] = MergeCookie(docu["Cookie"].ToString(), response.Headers["set-cookie"]);

                responseHeaders= response.Headers;
                statusCode = response.StatusCode;
                objhttpitem.Parameters = HttpItem.HeaderToString(docu);
                //GZIIP处理
                if (response.ContentEncoding != null &&
                    response.ContentEncoding.Equals("gzip", StringComparison.InvariantCultureIgnoreCase))
                {
                    stream =
                        GetMemoryStream(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress));
                }
                else
                {
                    stream = GetMemoryStream(response.GetResponseStream());
                }
                //获取Byte
                var rawResponse = stream.ToArray();
                //是否返回Byte类型数据

                if (objhttpitem.Encoding == EncodingType.Unknown || encoding == null)
                {
                    var temp = Encoding.Default.GetString(rawResponse, 0, rawResponse.Length);
                    //<meta(.*?)charset([\s]?)=[^>](.*?)>
                    var meta = Regex.Match(temp, "<meta([^<]*)charset=([^<]*)[\"']",
                        RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    var charter = (meta.Groups.Count > 2) ? meta.Groups[2].Value : string.Empty;
                    charter = charter.Replace("\"", string.Empty)
                        .Replace("'", string.Empty)
                        .Replace(";", string.Empty);
                    if (charter.Length > 0)
                    {
                        charter = charter.ToLower().Replace("iso-8859-1", "gbk");
                        if (charter.Contains("utf-8") || charter.Contains("UTF-8"))
                        {
                            encoding = Encoding.UTF8;
                        }
                        else if (charter.Contains("gb"))
                        {
                            encoding = Encoding.GetEncoding("GB2312");
                        }
                        else
                        {
                            encoding = Encoding.GetEncoding(charter);
                        }
                    }
                    else
                    {
                        if (response.CharacterSet != null && response.CharacterSet.ToLower().Trim() == "iso-8859-1")
                        {
                            encoding = Encoding.GetEncoding("gbk");
                        }
                    }
                }


                //得到返回的HTML
                result = encoding.GetString(rawResponse);
                //最后释放流
                stream.Close();
            }

            return result;
        }

        /// <summary>
        ///     获取html代码
        /// </summary>
        /// <param name="url">网页地址</param>
        /// <returns></returns>
        public static string GetWebSourceHtml(string url, string strFormat = "gb2312")
        {
            var str = string.Empty;
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(url);


                request.Timeout = 30000;
                request.Headers.Set("Pragma", "no-cache");
                var response = request.GetResponse();
                var streamReceive = response.GetResponseStream();
                var encoding = Encoding.GetEncoding(strFormat);
                var streamReader = new StreamReader(streamReceive, encoding);
                str = streamReader.ReadToEnd();
                streamReader.Close();
            }
            catch (Exception ex)
            {
                XLogSys.Print.Error(ex.Message);
            }
            return str;
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetCookie(string lpszUrlName, string lbszCookieName, string lpszCookieData);


        /// <summary>
        ///     组装普通文本请求参数。
        /// </summary>
        /// <param name="parameters">Key-Value形式请求参数字典。</param>
        /// <returns>URL编码后的请求数据。</returns>
        private static string BuildPostData(IDictionary<string, object> parameters)
        {
            var postData = new StringBuilder();
            var hasParam = false;

            var dem = parameters.GetEnumerator();
            while (dem.MoveNext())
            {
                var name = dem.Current.Key;
                var value = dem.Current.Value.ToString();
                // 忽略参数名或参数值为空的参数
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    if (hasParam)
                    {
                        postData.Append("&");
                    }

                    postData.Append(name);
                    postData.Append("=");
                    postData.Append(Uri.EscapeDataString(value));
                    hasParam = true;
                }
            }

            return postData.ToString();
        }

        public static bool SetHeaderValue(WebHeaderCollection header, string name, string value)
        {
            var property = typeof (WebHeaderCollection).GetProperty("InnerCollection",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var collection = property?.GetValue(header, null) as NameValueCollection;
            if (collection != null)
            {
                collection[name] = value;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     4.0以下.net版本取数据使用
        /// </summary>
        /// <param name="streamResponse">流</param>
        private static MemoryStream GetMemoryStream(Stream streamResponse)
        {
            var _stream = new MemoryStream();
            var Length = 256;
            var buffer = new byte[Length];
            var bytesRead = streamResponse.Read(buffer, 0, Length);
            // write the required bytes  
            while (bytesRead > 0)
            {
                _stream.Write(buffer, 0, bytesRead);
                bytesRead = streamResponse.Read(buffer, 0, Length);
            }
            return _stream;
        }



        public static void SetRequest(HttpItem item, HttpWebRequest request, string desturl = null, string post = null)
        {
            var docu = item.GetHeaderParameter();
            // 设置代理
            if (item.ProxyPort == 0 || string.IsNullOrEmpty(item.ProxyIP))
            {
                //不需要设置
            }
            else
            {
                //设置代理服务器
                var myProxy = new WebProxy(item.ProxyIP, item.ProxyPort);

                //建议连接
                myProxy.Credentials = new NetworkCredential(item.ProxyUserName, item.ProxyPassword);
                //给当前请求对象
                request.Proxy = myProxy;
                //设置安全凭证
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            //请求方式Get或者Post
            request.Method = item.Method.ToString();
            request.Timeout = item.Timeout;
            request.ReadWriteTimeout = item.ReadWriteTimeout;
            //Accept

            request.Headers = new WebHeaderCollection();
            if (docu["Headers"].ToString() != "")
            {
                var str = docu["Headers"].ToString().Split('\n');
                foreach (var s in str)
                {
                    var ms = s.Split(':');
                    if (ms.Length != 2)
                        continue;
                    var key = ms[0].Trim();
                    var value = ms[1].Trim();
                    if (SetHeaderValue(request.Headers, key, value) == false)
                    {
                        request.Headers.Add(key, value);
                    }
                }
            }
            request.Accept = docu["Accept"].ToString();

            //ContentType返回类型
            request.ContentType = docu["Content_Type"].ToString();
            //UserAgent客户端的访问类型，包括浏览器版本和操作系统信息
            request.UserAgent = docu["User-Agent"].ToString();
            var host = docu["Host"].ToString();
            //if (string.IsNullOrEmpty(host) == false)
            // request.Host = host;

            //设置Cookie
            var cookie = docu["Cookie"].ToString();
            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers[HttpRequestHeader.Cookie] = cookie;
            }


            //来源地址
            request.Referer = docu["Referer"].ToString();
            //是否执行跳转功能
            request.AllowAutoRedirect = item.Allowautoredirect;
            //设置Post数据
            string postdata = null;
            if (post == null)
            {
                postdata = item.Postdata;
            }
            else
            {
                postdata = post;
            }
            //验证在得到结果时是否有传入数据
            if (!string.IsNullOrEmpty(postdata) && request.Method.Trim().ToLower().Contains("post"))
            {
                var buffer = Encoding.Default.GetBytes(postdata);
                request.ContentLength = buffer.Length;
                request.GetRequestStream().Write(buffer, 0, buffer.Length);
            }
            ////设置最大连接
            //if (item.Connectionlimit > 0)
            //{
            //    request.ServicePoint.ConnectionLimit = item.Connectionlimit;
            //}
        }

        /// <summary>
        ///     为请求准备参数
        /// </summary>
        /// <param name="item">参数列表</param>
        /// <param name="_Encoding">读取数据时的编码方式</param>
        private HttpWebRequest SetRequest(HttpItem item, string desturl = null, string post = null)
        {
            var url = desturl ?? item.URL;
            if (url == null)
                return null;
            if (url.Contains("http") == false)
            {
                url = "http://" + url;
            }
          
            //初始化对像，并设置请求的URL地址
            var request = (HttpWebRequest) WebRequest.Create(GetUrl(url));
            // 验证证书
            if (url.Contains("https"))
            {
                
                .ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;
                //ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls|(SecurityProtocolType)768|(SecurityProtocolType)3072;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            SetRequest(item, request, desturl, post);
            encoding = AttributeHelper.GetEncoding(item.Encoding);
            return request;
        }


        ///// <summary>
        /////     设置代理
        ///// </summary>
        ///// <param name="requestitem">参数对象</param>
        //private void SetProxy(HttpItem requestitem)
        //{
        //    if (string.IsNullOrEmpty(requestitem.ProxyUserName) && string.IsNullOrEmpty(requestitem.ProxyPwd) &&
        //        string.IsNullOrEmpty(requestitem.ProxyIp))
        //    {
        //        //不需要设置
        //    }
        //    else
        //    {
        //        //设置代理服务器
        //        var myProxy = new WebProxy(requestitem.ProxyIp, requestitem.ProxyPort);

        //        //建议连接
        //        myProxy.Credentials = new NetworkCredential(requestitem.ProxyUserName, requestitem.ProxyPwd);
        //        //给当前请求对象
        //        request.Proxy = myProxy;
        //        //设置安全凭证
        //        request.Credentials = CredentialCache.DefaultNetworkCredentials;
        //    }
        //}

        /// <summary>
        ///     回调验证证书问题
        /// </summary>
        /// <param name="sender">流对象</param>
        /// <param name="certificate">证书</param>
        /// <param name="chain">X509Chain</param>
        /// <param name="errors">SslPolicyErrors</param>
        /// <returns>bool</returns>
        public bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors errors)
        {
            // 总是接受    
            return true;
        }

        #endregion

        #region 普通类型

        /// <summary>
        ///     传入一个正确或不正确的URl，返回正确的URL
        /// </summary>
        /// <param name="URL">url</param>
        /// <returns>
        /// </returns>
        public static string GetUrl(string URL)
        {
            if (!(URL.Contains("http://") || URL.Contains("https://")))
            {
                URL = "http://" + URL;
            }
            return URL;
        }


        public static string GetRealIp()
        {
            var ip = "";
            try
            {
                var request = HttpContext.Current.Request;

                if (request.ServerVariables["http_VIA"] != null)
                {
                    ip = request.ServerVariables["http_X_FORWARDED_FOR"].Split(',')[0].Trim();
                }
                else
                {
                    ip = request.UserHostAddress;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return ip;
        }

        public static bool IsSuccess(HttpStatusCode code)
        {
            return code == HttpStatusCode.OK;
        }

        /// <summary>
        ///     采用https协议访问网络,根据传入的URl地址，得到响应的数据字符串。
        /// </summary>
        /// <param name="requestitem">参数列表</param>
        /// <returns>String类型的数据</returns>
        public string GetHtml(HttpItem requestitem, out WebHeaderCollection responseHeaders, out HttpStatusCode code, string url = null, string post = null)
        {
            try
            {
                var request = SetRequest(requestitem, url, post);
                var r = GetHttpRequestData(request, requestitem,out responseHeaders, out code);
                if (!IsSuccess(code))
                {
                    if(code==HttpStatusCode.Forbidden)
                        RequestManager.Instance.ForbidCount++;
                     if(code==HttpStatusCode.RequestTimeout||code==HttpStatusCode.GatewayTimeout)

                        RequestManager.Instance.TimeoutCount++;
                    
                    XLogSys.Print.Warn($"HTTP Request Failed {code} | {requestitem.URL} ");
                    return "HTTP错误，类型:" + code;

                }
                XLogSys.Print.Debug($"HTTP Request Success {code} | {requestitem.URL} ");
                return r;
            }
            catch (Exception ex)
            {
                code = HttpStatusCode.NotFound;
                responseHeaders = null;
                return ex.Message;
            }
        }

        /// <summary>
        ///     采用https协议访问网络,根据传入的URl地址，得到响应的数据字符串。
        /// </summary>
        /// <param name="requestitem">参数列表</param>
        /// <returns>String类型的数据</returns>
        public string GetHtml(HttpItem requestitem,  out HttpStatusCode code, string url = null, string post = null)
        {
            WebHeaderCollection responseHeaders = null;
            return GetHtml(requestitem, out responseHeaders,out  code, url, post);
        }

        /// <summary>
        ///     采用https协议访问网络,根据传入的URl地址，得到响应的数据字符串。
        /// </summary>
        /// <param name="requestitem">参数列表</param>
        /// <returns>String类型的数据</returns>
        public byte[] GetFile(HttpItem requestitem, out HttpStatusCode code, string url = null, string post = null)
        {
            try
            {
                var request = SetRequest(requestitem, url, post);
                var r = GetHttpRequestFile(request, requestitem, out code);
                if (!IsSuccess(code))
                    XLogSys.Print.ErrorFormat("HTTP错误，URL:{0},类型:{1}", url, code.ToString());
                return r;
            }
            catch (Exception ex)
            {
                code = HttpStatusCode.NotFound;
                return new byte[0];
            }
        }

        #endregion
    }

    public enum MethodType
    {
        GET,
        POST
    }


    /// <summary>
    ///     返回类型
    /// </summary>
    public enum ResultType
    {
        String, //表示只返回字符串
        Byte //表示返回字符串和字节流
    }
}
