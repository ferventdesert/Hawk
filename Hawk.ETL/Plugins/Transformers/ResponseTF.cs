using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;
using HtmlAgilityPack;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("获取请求响应", "使用网页采集器获取网页数据，得到响应字段的值并添加到对应的属性中")]
    public class ResponseTF : TransformerBase
    {
        protected readonly BuffHelper<HtmlDocument> buffHelper = new BuffHelper<HtmlDocument>(50);
        private readonly HttpHelper helper = new HttpHelper();
        protected SmartCrawler _crawler;

        public ResponseTF()
        {
            CrawlerSelector = new TextEditSelector();
            CrawlerSelector.GetItems = this.GetAllCrawlerNames();
        }

        [LocalizedDisplayName("爬虫选择")]
        [LocalizedDescription("填写采集器或模块的名称")]
        public TextEditSelector CrawlerSelector { get; set; }

        [LocalizedDisplayName("响应头")]
        [LocalizedDescription("要获取的响应头的名称，多个之间用空格分割，不区分大小写")]
        public virtual string HeaderFilter { get; set; }

        [Browsable(false)]
        public SmartCrawler Crawler
        {
            get { return _crawler; }
            set
            {
                if (_crawler != value)
                {
                    if (_crawler != null)
                        Crawler.PropertyChanged -= CrawlerPropertyChangedHandler;
                    value.PropertyChanged += CrawlerPropertyChangedHandler;
                    _crawler = value;
                }
            }
        }

        private void CrawlerPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            buffHelper.Clear();
        }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            OneOutput = false;
            Crawler = GetCrawler(CrawlerSelector.SelectItem);
            if (string.IsNullOrEmpty(CrawlerSelector.SelectItem) && Crawler != null)
                CrawlerSelector.SelectItem = Crawler.Name;
            return Crawler != null && base.Init(datas);
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

            var content = helper.GetHtml(Crawler.Http, out responseHeader, out code, url);
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