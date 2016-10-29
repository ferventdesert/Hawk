using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;
using HtmlAgilityPack;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("转换为Json", "从字符串转换为json（数组或字典类型）")]
    public class JsonTF : TransformerBase
    {
        private readonly JavaScriptSerializer serialier;
        private string lastData;

        public JsonTF()
        {
            serialier = new JavaScriptSerializer();
            ScriptWorkMode = ScriptWorkMode.不进行转换;
            OneOutput = false;
        }

        [LocalizedDisplayName("工作模式")]
        [LocalizedDescription("文档列表：[{}],转换为多个数据行构成的列表；单文档：{},将结果的键值对附加到本行；不进行转换：直接将值放入到新列")]
        public ScriptWorkMode ScriptWorkMode { get; set; }



        [LocalizedCategory("使用采集器协助提取Json")]
        [LocalizedDisplayName("采集器名称")]
        [LocalizedDescription("填写采集器的名称")]
        public string CrawlerSelector { get; set; }

        [LocalizedCategory("使用采集器协助提取Json")]
        [LocalizedDisplayName("启用")]
        public bool CrawlerEnabled { get; set; }

        [LocalizedCategory("使用采集器协助提取Json")]
        [LocalizedDisplayName("执行")]
        [PropertyOrder(20)]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("采集器设计", obj => Design())
                        //     new Command("模拟登录", obj => { AutoVisit(); })
                    });
            }
        }

        private SmartCrawler selector;
        private bool crawlerEnabled = false;
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            crawlerEnabled = false;
            if (CrawlerEnabled)
            {
                selector = GetCrawler(CrawlerSelector);
                if (selector != null)
                {
                    crawlerEnabled = true;
                    IsMultiYield = selector.IsMultiData == ListType.List;
                }
            }
            else
            {
                IsMultiYield = ScriptWorkMode == ScriptWorkMode.文档列表;
            }
            lastData = null;
            return base.Init(docus);
        }

        public override object TransformData(IFreeDocument datas)
        {
            var item = datas[Column];
            if (item == null || string.IsNullOrWhiteSpace(item.ToString()))
                return null;
            bool isrealjson;
            var itemstr = item.ToString();
            if (lastData == null)
            {
                var html = XPathAnalyzer.Json2XML(itemstr, out isrealjson, true);
                if(isrealjson)
                    lastData = itemstr;

            }
            
            if (crawlerEnabled)
            {
             
                var html = XPathAnalyzer.Json2XML(itemstr, out isrealjson, true);
                if (isrealjson)
                {
                    HtmlDocument htmldoc = null;
                    var doc = selector.CrawlHtmlData(html, out htmldoc).FirstOrDefault();
                        doc.DictCopyTo(datas);
                }
                return null;
            }
            dynamic d = null;
            try
            {
                d = serialier.DeserializeObject(item.ToString());
            }
            catch (Exception ex)
            {
                SetValue(datas, ex.Message);
                // XLogSys.Print.Error(ex);
                return null;
            }
            if (ScriptWorkMode == ScriptWorkMode.单文档)
            {
                var newdoc = ScriptHelper.ToDocument(d) as FreeDocument;
                newdoc.DictCopyTo(datas);
            }
            else
            {
                SetValue(datas, d);
            }

            return null;
        }

        private void Design()
        {
            (!string.IsNullOrWhiteSpace(CrawlerSelector)).SafeCheck("采集器名称不能为空");

            var isRealJson = false;
            var newhtml = XPathAnalyzer.Json2XML(lastData, out isRealJson, true);
            if (!(isRealJson).SafeCheck("只有标准json格式才能启用采集器设计"))
                return;
            var selector = GetCrawler(CrawlerSelector); 
            if (selector == null)
            {
                if (MessageBox.Show($"是否要创建名为{CrawlerSelector}的网页采集器?", "提示信息", MessageBoxButton.OKCancel) !=
                    MessageBoxResult.OK)
                {
                    return;
                }
                var crawler = new SmartCrawler();
                crawler.Name = CrawlerSelector;
                processManager.CurrentProcessCollections.Add(crawler);
                selector = crawler;
            }

            (MainDescription.MainFrm as IDockableManager).ActiveThisContent(CrawlerSelector);
            selector.URLHTML = newhtml;
            selector.HtmlDoc.LoadHtml(newhtml);
            selector.enableRefresh = false;
            //selector.GreatHand();
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var item = data[Column].ToString();
                if (string.IsNullOrEmpty(item))
                    continue;
                var itemstr = item;
                lastData = itemstr;
                if (crawlerEnabled)
                {
                    bool isrealjson;
                    var html = XPathAnalyzer.Json2XML(itemstr, out isrealjson, true);
                    if (isrealjson)
                    {
                        HtmlDocument htmldoc = null;
                        var doc = selector.CrawlHtmlData(html, out htmldoc);
                        foreach (var item3 in doc)
                        {
                            yield return item3.MergeQuery(data, NewColumn);

                        }

                    }
                    continue;
                }
                dynamic d = null;
                try
                {
                    d = serialier.DeserializeObject(itemstr);
                }
                catch (Exception ex)
                {
                    //  XLogSys.Print.Error(ex);
                    continue;
                }


                foreach (var item2 in ScriptHelper.ToDocuments(d))
                {
                    var item3 = item2 as FreeDocument;
                    yield return item3.MergeQuery(data, NewColumn);
                }
            }
        }
    }
}