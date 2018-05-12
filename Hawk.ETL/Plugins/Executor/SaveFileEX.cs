using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("保存超链接文件", "目标列需要为超链接类型，会保存链接的文件，如图片，视频等","save")]
    public class SaveFileEX : DataExecutorBase
    {
        private string _crawlerSelector;
        private HttpHelper helper;

        public SaveFileEX()
        {
            CrawlerSelector = new TextEditSelector
            {
                GetItems = () =>
                {
                    return
                        processManager.CurrentProcessCollections.Where(d => d is SmartCrawler)
                            .Select(d => d.Name)
                            .ToList();
                }
            };
        }

        [LocalizedDisplayName("保存位置")]
        [LocalizedDescription("路径或文件名，例如D:\\file.txt, 可通过'[]'引用其他列， \n 若为目录名，必须显式以/结束，文件名将会通过url自动解析")]
        public string SavePath { get; set; }

        [LocalizedDisplayName("爬虫选择")]
        [LocalizedDescription("填写采集器或模块的名称")]
        public TextEditSelector CrawlerSelector { get; set; }

        [LocalizedDisplayName("是否异步")]
        public bool IsAsync { get; set; }

        private SmartCrawler crawler { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            crawler =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == CrawlerSelector.SelectItem) as
                    SmartCrawler;
            if (crawler != null)
            {
            }
            else
            {
                var task = processManager.CurrentProject.Tasks.FirstOrDefault(d => d.Name == CrawlerSelector.SelectItem);
                if (task == null)
                    return false;
                ControlExtended.UIInvoke(() => { task.Load(false); });
                crawler =
                    processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == CrawlerSelector.SelectItem)
                        as
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
                var isdir = IsDir(path);
                var url = document[Column]?.ToString();
                if (string.IsNullOrEmpty(url))
                {
                    yield return document;
                    continue;
                }
                DirectoryInfo folder = null;
                if (!isdir)
                {
                    folder = directoryInfo.Parent;
                }
                else
                {
                    folder = directoryInfo;
                }

                if (folder == null)
                {
                    yield return document;
                    continue;
                }
                if (!folder.Exists)
                {
                    folder.Create();
                }
                if (isdir)
                {
                    path = folder.ToString();
                    if (path.EndsWith("/") == false)
                        path += "/";
                    path += url;
                    path = getFileName(path);
                    //path += getFileName(url);
                }
                if (File.Exists(path))
                    {
                        yield return document;
                        continue;
                    }


                try
                {
                    var webClient = new CookieAwareWebClient();
                    if (!IsAsync)
                    {
                        webClient.DownloadFile(url, path);
                    }
                    else
                    {
                        webClient.DownloadFileAsync(new Uri(url), path);
                    }
                    //HttpStatusCode code;
                    //var bytes = helper.GetFile(crawler.Http, out code, url);
                    //if (bytes != null)
                    //    File.WriteAllBytes(path, bytes);
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error(ex);
                }

                yield return document;
            }
        }

        /// <summary>
        ///     判断目标是文件夹还是目录(目录包括磁盘)
        /// </summary>
        /// <param name="filepath">文件名</param>
        /// <returns></returns>
        public static bool IsDir(string filepath)
        {
            if (filepath.EndsWith("\\"))
            {
                return true;
            }
            var fi = new FileInfo(filepath);
            if (fi.Exists && (fi.Attributes & FileAttributes.Directory) != 0)
                return true;
            return false;
        }

        public static string getFileName(string path)
        {
            var str = string.Empty;
            var pos1 = path.LastIndexOf('/');
            var pos2 = path.LastIndexOf('\\');
            var pos = Math.Max(pos1, pos2);
            if (pos < 0)
                str = path;
            else
                str = path.Substring(pos + 1);
            var chars = @"/\/:*?""< >|\t";
            foreach (var item in chars)
            {
                str = str.Replace(item.ToString(), "");
            }
            return str;
        }
    }
}