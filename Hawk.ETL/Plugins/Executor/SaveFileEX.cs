using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("保存超链接文件", "目标列需要为超链接类型，会保存链接的文件，如图片，视频等")]
    public class SaveFileEX : DataExecutorBase
    {
        [DisplayName("保存位置")]
        public string SavePath { get; set; }

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
                    var mywebclient = new WebClient();
                    mywebclient.DownloadFile(url, path);
                }
                catch (Exception ex)
                {
                    XLogSys.Print.ErrorFormat("下载文件错误，url为{0},异常为{1}", url, ex.Message);
                }

                yield return document;
            }
        }
    }
}