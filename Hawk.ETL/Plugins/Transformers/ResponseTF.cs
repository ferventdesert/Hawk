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
    [XFrmWork("ResponseTF", "ResponseTF_desc")]
    public class ResponseTF : TransformerBase
    {
        protected readonly BuffHelper<HtmlDocument> buffHelper = new BuffHelper<HtmlDocument>(50);
        private readonly HttpHelper helper = new HttpHelper();
        protected SmartCrawler _crawler;

        public ResponseTF()
        {
            CrawlerSelector = new TextEditSelector();
            CrawlerSelector.GetItems = this.GetAllCrawlerNames();
            IsMultiYield = false;
        }

        [PropertyOrder(1)]
        [LocalizedDisplayName("key_482")]
        public string PostData { get; set; }

        [PropertyOrder(2)]
        [LocalizedDisplayName("key_118")]
        public string Proxy { get; set; }
        [LocalizedDisplayName("key_359")]
        [LocalizedDescription("key_360")]
        public TextEditSelector CrawlerSelector { get; set; }

        [LocalizedDisplayName("key_529")]
        [LocalizedDescription("key_530")]
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
     
            var name = CrawlerSelector.SelectItem;
            name = AppHelper.Query(name, null);
            Crawler = GetCrawler(name);
            if (string.IsNullOrEmpty(CrawlerSelector.SelectItem) && Crawler != null)
                CrawlerSelector.SelectItem = Crawler.Name;
            IsMultiYield = false;
            return  base.Init(datas);
        }

        public override
            object TransformData(IFreeDocument datas)
        {

         
            var p = datas[Column];
            var post = datas.Query(PostData);
            if (p == null)
                return new List<FreeDocument>();
            var url = p.ToString();
            Crawler.SetCookie(Crawler.Http);
            var response=  helper.GetHtml(Crawler.Http, url,post).Result;

            var content = response.Html;
            var code = response.Code;
            var responseHeader = response.ResponseHeaders;
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