using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Process;
using HtmlAgilityPack;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("CrawlerTF", "CrawlerTF_desc", "carema")]
    public class CrawlerTF : ResponseTF
    {

        public CrawlerTF()
        {
            PropertyChanged += (s, e) => {
                if (e.PropertyName == "AnalyzeItem" || e.PropertyName == "")
                    return;
                    buffHelper.Clear(); };
        }
        static  SmartCrawler defaultCrawler=new SmartCrawler();

        [Browsable(false)]
        public override string HeaderFilter { get; set; }

      
        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            base.Init(datas);

            IsMultiYield = Crawler?.IsMultiData == ScriptWorkMode.List && Crawler.CrawlItems.Count > 0;
            if(IsMultiYield)
            {
                buffHelper.SetBuffSize(5);
            }
            return true;
        }

        public bool ProcessHtmlOnly { get; set; }

        [Browsable(false)]
        public override string KeyConfig => CrawlerSelector.SelectItem;
        private IEnumerable<FreeDocument> GetDatas(IFreeDocument data)
        {
            var p = data[Column];
            if (p == null || Crawler == null)
                return new List<FreeDocument>();
            var urlOrHtml = p.ToString();
            var bufkey = urlOrHtml;
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
                IEnumerable<FreeDocument> docs = null;
                HttpStatusCode code;
                if (!ProcessHtmlOnly)
                {
                    docs = crawler.CrawlData(urlOrHtml, out htmldoc, out code, post);
                    var any = docs.Any();
                    if (HttpHelper.IsSuccess(code))
                    {
                        if (!any)
                        {
                            ConfigFile.GetConfig<DataMiningConfig>().ParseErrorCount++;
                            throw new Exception(string.Format(GlobalHelper.Get("key_669"), urlOrHtml));
                        }
                        if (this.IsExecute == false)
                            buffHelper.Set(bufkey, htmldoc);
                        return docs;
                    }
                    throw new Exception("Web Request Error:" + code);
                }
                else
                {
                    docs = crawler.CrawlHtmlData(urlOrHtml, out htmldoc);
                    return docs;
                }


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