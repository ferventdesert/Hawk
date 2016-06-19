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
    [XFrmWork("获取请求详情", "使用网页采集器获取网页数据，拖入的列需要为超链接")]
    public class ResponseTF : TransformerBase
    {
        protected readonly BuffHelper<HtmlDocument> buffHelper = new BuffHelper<HtmlDocument>(50);
        protected readonly IProcessManager processManager;
        private string _crawlerSelector;
        private BfsGE generator;
        private bool isfirst;

        public ResponseTF()
        {
            processManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;
            //  var defaultcraw = processManager.CurrentProcessCollections.FirstOrDefault(d => d is SmartCrawler);
            OneOutput = false;
            //if (defaultcraw != null) CrawlerSelector = defaultcraw.Name;
            PropertyChanged += (s, e) => { buffHelper.Clear(); };
        }
        private  HttpHelper helper=new HttpHelper();
        public override object TransformData(IFreeDocument datas)
        {
            HttpStatusCode code;
            WebHeaderCollection responseHeader;
            var http = helper.GetHtml(crawler.Http,out responseHeader, out code, datas[Column].ToString());
            var keys = HeaderFilter.Split(' ');
            foreach (var key in keys)
            {
                if (responseHeader.AllKeys.Contains(key))
                    datas.SetValue(key, responseHeader[key]);
            }
            return null;
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



        [LocalizedCategory("头数据")]
        public virtual string HeaderFilter { get; set; }
        protected SmartCrawler crawler { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
                      crawler =
         processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == CrawlerSelector) as SmartCrawler;
            if (crawler != null)
            {
                IsMultiYield = crawler?.IsMultiData == ListType.List;
            }
            else
            {
                var task = processManager.CurrentProject.Tasks.FirstOrDefault(d => d.Name == CrawlerSelector);
                if (task == null)
                    return false;
                ControlExtended.UIInvoke(() => { task.Load(false); });
                crawler =
                    processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == CrawlerSelector) as
                        SmartCrawler;
            }




          
            return crawler != null && base.Init(datas);
        }

     
    }
}