using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                var url = document[Column].ToString();
                if(string.IsNullOrEmpty(url))
                    continue;
              try
                {
                    WebClient mywebclient = new WebClient();
                    mywebclient.DownloadFile(url, path);
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
