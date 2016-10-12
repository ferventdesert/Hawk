using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Hawk.Core.Utils
{
   public class CookieAwareWebClient : WebClient

    {

            public string Method;
            public HttpItem  HttpItem { get; set; }
            public Uri Uri { get; set; }


            public CookieAwareWebClient()
              
            {
            }

            public CookieAwareWebClient(HttpItem item)
            {
                this.Encoding = Encoding.UTF8;
                HttpItem = item;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request is HttpWebRequest)
                {
                   if(HttpItem!=null)
                     HttpHelper.SetRequest(HttpItem,request as HttpWebRequest);

                }
                HttpWebRequest httpRequest = (HttpWebRequest)request;
                httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                return httpRequest;
            }

          
    }
}
