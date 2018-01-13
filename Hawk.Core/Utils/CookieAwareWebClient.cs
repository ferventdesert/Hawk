using System;
using System.Net;
using System.Text;

namespace Hawk.Core.Utils
{
    public class CookieAwareWebClient : WebClient

    {
        private readonly CookieContainer cookie = new CookieContainer();
        public string Method;

        public CookieAwareWebClient()

        {
        }

        public CookieAwareWebClient(HttpItem item)
        {
            Encoding = Encoding.UTF8;
            HttpItem = item;
        }

        public HttpItem HttpItem { get; set; }
        public Uri Uri { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                if (HttpItem != null)
                    HttpHelper.SetRequest(HttpItem, request as HttpWebRequest);
                (request as HttpWebRequest).CookieContainer = cookie;
            }
            //HttpWebRequest httpRequest = (HttpWebRequest)request;
            //httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }
    }
}