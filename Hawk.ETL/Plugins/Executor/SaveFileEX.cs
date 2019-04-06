using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("SaveFileEX", "SaveFileEX_desc","save")]
    public class SaveFileEX : DataExecutorBase
    {

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

        [LocalizedDisplayName("key_357")]
        [LocalizedDescription("key_358")]
        public string SavePath { get; set; }

        [LocalizedDisplayName("key_359")]
        [LocalizedDescription("key_360")]
        public TextEditSelector CrawlerSelector { get; set; }

        [LocalizedDisplayName("key_361")]
        public bool IsAsync { get; set; }

        private SmartCrawler crawler { get; set; }

        [Browsable(false)]
        public override string KeyConfig => SavePath;
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
                   if (path.EndsWith("\\") == false)
                        path += "\\";
                    //path += url;
                    //path = getFileName(path);
                    path += getFileName(url);
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