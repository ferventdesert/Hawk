using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Process;
using HtmlAgilityPack;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("获取请求响应", "使用网页采集器获取网页数据，并得到对应的响应字段")]
    public class ResponseTF : TransformerBase
    {
        protected readonly BuffHelper<HtmlDocument> buffHelper = new BuffHelper<HtmlDocument>(50);
        private readonly HttpHelper helper = new HttpHelper();
        protected string _crawlerSelector;

        public ResponseTF()
        {
            CrawlerSelector = "网页采集器";
        }

        [LocalizedDisplayName("爬虫选择")]
        [LocalizedDescription("填写采集器或模块的名称")]
        public string CrawlerSelector
        {
            get { return _crawlerSelector; }
            set
            {
                if (_crawlerSelector != value)
                {
                    buffHelper?.Clear();
                }
                _crawlerSelector = value;
            }
        }

        public virtual string HeaderFilter { get; set; }
        protected SmartCrawler crawler { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            OneOutput = false;
            crawler = GetCrawler(CrawlerSelector);
            return crawler != null && base.Init(datas);
        }

        public override
            object TransformData(IFreeDocument datas)
        {
            var p = datas[Column];
            if (p == null)
                return new List<FreeDocument>();
            var url = p.ToString();
            WebHeaderCollection responseHeader;
            HttpStatusCode code;

            var content = helper.GetHtml(crawler.Http, out responseHeader, out code, url);
            var keys = responseHeader.AllKeys;
            if (!string.IsNullOrEmpty(HeaderFilter))
            {
                keys = HeaderFilter.Split(' ');
            }
            foreach (var key in keys)
            {
                var value = responseHeader.Get(key);
                if (value != null)
                    datas.SetValue(key, value);
            }
            if (keys.Contains("Location") && datas.ContainsKey("Location") == false)
            {
                datas["Location"] = url;
            }

            return null;
        }
    }
}