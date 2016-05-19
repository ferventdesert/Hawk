using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("保存超链接文件", "目标列需要为超链接类型，会保存链接的文件，如图片，视频等")]
    public class SaveFileEX : DataExecutorBase
    {
        private  HttpHelper helper;
        private readonly IProcessManager processManager;

        public SaveFileEX()
        {
            processManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;
        }

        [DisplayName("保存位置")]
        public string SavePath { get; set; }

        [DisplayName("爬虫选择")]
        [Description("填写采集器或模块的名称")]
        public string CrawlerSelector { get; set; }

        private SmartCrawler crawler { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            crawler =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == CrawlerSelector) as SmartCrawler;
            if (crawler != null)
            {
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
            helper = new HttpHelper();
            return base.Init(datas);
        }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            foreach (var document in documents)
            {
                var path = document.Query(SavePath);
                var directoryInfo = new DirectoryInfo(path);
                var folder = directoryInfo.Parent;
                if (folder == null)
                    continue;
                if (!folder.Exists)
                {
                    folder.Create();
                }
                var url = document[Column].ToString();
                if (string.IsNullOrEmpty(url))
                    continue;
                try
                {
                    HttpStatusCode code;
                    var bytes = helper.GetFile(crawler.Http, out code, url);
                    if (bytes != null)
                        File.WriteAllBytes(path, bytes);
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error(ex);
                }

                yield return document;
            }
        }
    }
}