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
using Hawk.ETL.Plugins.Generators;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("从爬虫转换", "使用网页采集器获取网页数据，拖入的列需要为超链接")]
    public class CrawlerTF : ResponseTF
    {
        private BfsGE generator;
        private bool isfirst;
        private Regex regex;

        public CrawlerTF()
        {
            //  var defaultcraw = processManager.CurrentProcessCollections.FirstOrDefault(d => d is SmartCrawler);
            MaxTryCount = "1";
            ErrorDelay = 3000;
            SetPrefex = "";
            //if (defaultcraw != null) CrawlerSelector = defaultcraw.Name;
            PropertyChanged += (s, e) => { buffHelper.Clear(); };
        }

        [LocalizedCategory("高级设置")]
        [LocalizedDisplayName("最大重复次数")]
        public string MaxTryCount { get; set; }

        [LocalizedCategory("高级设置")]
        [LocalizedDisplayName("延时时间")]
        public string DelayTime { get; set; }

        [LocalizedCategory("高级设置")]
        [LocalizedDisplayName("错误延时时间")]
        public int ErrorDelay { get; set; }

        [LocalizedDisplayName("Post数据")]
        public string PostData { get; set; }

        [Browsable(false)]
        [LocalizedCategory("请求队列")]
        [LocalizedDisplayName("队列生成器")]
        [LocalizedDescription("填写模块的名称")]
        public string GEName { get; set; }

        [Browsable(false)]
        [LocalizedCategory("请求队列")]
        [LocalizedDisplayName("过滤规则")]
        public string Prefix { get; set; }

        [Browsable(false)]
        [LocalizedCategory("请求队列")]
        [LocalizedDisplayName("启用正则")]
        public bool IsRegex { get; set; }

        [LocalizedCategory("请求队列")]
        [Browsable(false)]
        [LocalizedDisplayName("添加前缀")]
        public string SetPrefex { get; set; }

        [Browsable(false)]
        public override string HeaderFilter { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            

            base.Init(datas);

            IsMultiYield = crawler?.IsMultiData == ListType.List && crawler.CrawlItems.Count>0;
            isfirst = true;

            if (IsRegex)
                regex = new Regex(Prefix);
            return crawler != null;
        }

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
                var maxcount = 1;
                int.TryParse(data.Query(MaxTryCount), out maxcount);

                var count = 0;
                while (count < maxcount)
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