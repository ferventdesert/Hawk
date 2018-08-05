using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("CrawlerTF", "CrawlerTF_desc", "carema")]
    public class CrawlerTF : ResponseTF
    {
        private BfsGE generator;

        public CrawlerTF()
        {
            ErrorDelay = 3000;
            PropertyChanged += (s, e) => { buffHelper.Clear(); };
        }
        static  SmartCrawler defaultCrawler=new SmartCrawler();

        [Browsable(false)]
        public override string HeaderFilter { get; set; }

        [LocalizedCategory("key_67")]
        [LocalizedDisplayName("key_481")]
        public int ErrorDelay { get; set; }

        [LocalizedDisplayName("key_482")]
        public string PostData { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);

            IsMultiYield = Crawler?.IsMultiData == ScriptWorkMode.List && Crawler.CrawlItems.Count > 0;
            return true;
        }

        private IEnumerable<FreeDocument> GetDatas(IFreeDocument data)
        {
            var p = data[Column];
            if (p == null || Crawler == null)
                return new List<FreeDocument>();
            var url = p.ToString();
            var bufkey = url;
            var post = data.Query(PostData);
            var crawler = Crawler;
            if (crawler == null)
            {
                crawler = defaultCrawler;
            }
            if (crawler.Http.Method == MethodType.POST)
            {
                bufkey += post;
            }
            var htmldoc = buffHelper.Get(bufkey);

            if (htmldoc == null)
            {
                var random= new Random();
                if(random.Next(0,100)>50)
                    throw  new Exception("网络请求错误");
                HttpStatusCode code;

                var count = 0;
                var docs = crawler.CrawlData(url, out htmldoc, out code, post);
                if (HttpHelper.IsSuccess(code))
                {
                    buffHelper.Set(bufkey, htmldoc);
                    return docs;
                }
                Thread.Sleep(ErrorDelay);
                throw new Exception("Web Request Error:" + code);
            }
            return crawler.CrawlData(htmldoc.DocumentNode);
        }

        protected override IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument data)
        {
            var docs = GetDatas(data);
            return docs.Select(d => d.MergeQuery(data, NewColumn));
        }

        public override object TransformData(IFreeDocument datas)
        {
            var docs = GetDatas(datas);
            var first = docs.FirstOrDefault();
            if (first != null)
            {
                first.DictCopyTo(datas);
            }

            return null;
        }
    }
}