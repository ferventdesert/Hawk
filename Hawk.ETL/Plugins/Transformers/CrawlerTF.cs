using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Process;
using HtmlAgilityPack;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("从爬虫转换", "使用网页采集器获取网页数据，拖入的列需要为超链接")]
    public class CrawlerTF : ResponseTF
    {
        
       
        private BfsGE generator;
        private bool isfirst;

        public CrawlerTF()
        {
          //  var defaultcraw = processManager.CurrentProcessCollections.FirstOrDefault(d => d is SmartCrawler);
            MaxTryCount = "1";
            ErrorDelay = 3000;
            SetPrefex = "";
            //if (defaultcraw != null) CrawlerSelector = defaultcraw.Name;
            PropertyChanged += (s, e) => { buffHelper.Clear(); };
        }

        [LocalizedDisplayName("最大重复次数")]
        public string MaxTryCount { get; set; }

        [LocalizedDisplayName("延时时间")]
        public string DelayTime { get; set; }

        [LocalizedDisplayName("错误延时时间")]
        public int  ErrorDelay { get; set; }

        [LocalizedDisplayName("Post数据")]
        public string PostData { get; set; }

      

        [LocalizedCategory("请求队列")]
        [LocalizedDisplayName("队列生成器")]
        [LocalizedDescription("填写模块的名称")]
        public string GEName { get; set; }

        [LocalizedCategory("请求队列")]
        [LocalizedDisplayName("过滤规则")]
        public string Prefix { get; set; }

        [LocalizedCategory("请求队列")]
        [LocalizedDisplayName("启用正则")]
        public bool IsRegex { get; set; }

        [LocalizedCategory("请求队列")]
        [LocalizedDisplayName("添加前缀")]
        public string SetPrefex { get; set; }

        private Regex regex;

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            if (generator == null)
            {
                var mainstream =
            processManager.CurrentProcessCollections.OfType<SmartETLTool>()
                .FirstOrDefault(d => d.CurrentETLTools.Contains(this));
                generator = mainstream.CurrentETLTools.FirstOrDefault(d => d.Name == GEName) as BfsGE;
            }


            base.Init(datas);

            IsMultiYield = crawler?.IsMultiData == ListType.List;
            isfirst = true;
         
            if(IsRegex)
                regex=new Regex(Prefix);
            return crawler != null ;
        }
        [Browsable(false)]
        public override string HeaderFilter { get; set; }

        private List<FreeDocument> GetDatas(IFreeDocument data)
        {
            var p = data[Column];
            if (p == null)
                return new List<FreeDocument>();
            var url = p.ToString();
            var bufkey = url;
            var post = data.Query(PostData);
            if (crawler.Http.Method == MethodType.POST)
            {
                bufkey += post;
            }
            var htmldoc = buffHelper.Get(bufkey);
            var docs = new List<FreeDocument>();
            if (htmldoc == null)
            {
                var delay = data.Query(DelayTime);
                var delaytime = 0;
                if (delay != null && int.TryParse(delay, out delaytime))
                {
                    if (delaytime != 0)
                        Thread.Sleep(delaytime);
                }

                HttpStatusCode code;
                int maxcount = 1;
                int.TryParse(data.Query(MaxTryCount),out maxcount);
                  
                int count = 0;
                while (count<maxcount)
                {
                    docs = crawler.CrawlData(url, out htmldoc, out code, post);
                    if (HttpHelper.IsSuccess(code))
                    {
                        buffHelper.Set(bufkey, htmldoc);
                        break;
                    }
                    Thread.Sleep(ErrorDelay);
                    count++;

                }
             
            
            }
            else
            {
                docs = crawler.CrawlData(htmldoc);
            }

            if (generator != null)
            {

                var others = htmldoc.DocumentNode.SelectNodes("//@href");

                var r3 = others.Select(d => d.Attributes["href"].Value).ToList();
                IEnumerable<string> r4;

                if (string.IsNullOrEmpty(Prefix))
                    r4 = r3;
              else  if(IsRegex==false)
                 r4 =
                    r3.Where(d => d.StartsWith(Prefix)).Where(d => true);
              else
              {
                  r4 = r3.Where(d => regex.IsMatch(d));
              }
                foreach (var href in r4)
                {
                    generator.InsertQueue(SetPrefex+href);
                }
            }
            return docs;
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var docs = GetDatas(data);
                foreach (var doc in docs)
                {
                    yield return doc.MergeQuery(data, NewColumn);
                }
            }
        }
            
        //private bool checkautoLogIn(List<FreeDocument> docs)

        //{
        //    if (docs.Count == 0 && isfirst)
        //    {
        //        if (crawler.Documents.Any())
        //        {
        //            crawler.AutoVisit();
        //            return false;
        //        }
        //        if (string.IsNullOrEmpty(crawler.URLFilter) == false &&
        //            crawler.IsRunning == false)
        //            crawler.StartVisit();
        //        return false;
        //    }
        //    if (docs.Count > 0 && crawler.IsRunning)
        //    {
        //        crawler.StopVisit();
        //    }
        //    isfirst = false;
        //    return true;
        //}

        public override object TransformData(IFreeDocument datas)
        {
            var docs = GetDatas(datas);
            if (docs.Count > 0)
            {
                var first = docs.First();
                first.DictCopyTo(datas);
            }

            return null;
        }
    }
}