using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;
using HtmlAgilityPack;

namespace Hawk.ETL.Plugins.Generators
{
    [XFrmWork("请求队列")]
    public class BfsGE : GeneratorBase
    {
        private  ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

        public BfsGE()
        {
        
            URLHash = new SortedSet<int>();
        }

        private SortedSet<int> URLHash { get; }

        [LocalizedDisplayName("BFS起始位置")]
        public string StartURL { get; set; }

        [LocalizedDisplayName("延时时间")]
        public int DelayTime { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
          

            URLHash.Clear();
            queue = new ConcurrentQueue<string>();
            return true;
        }


        public void InsertQueue(string url)
        {
            if (url == null)
                return;
            if (!URLHash.Contains(url.GetHashCode()))
            {
                queue.Enqueue(url);
            }
        }
        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {

            InsertQueue(StartURL);

            while (Enabled)
            {
                string currentURL;

                if (queue.TryDequeue(out currentURL))
                {
                
                    var urlhash = currentURL.GetHashCode();
                    if (StartURL != currentURL && URLHash.Contains(urlhash))
                    {
                        continue;
                    }
                    URLHash.Add(urlhash);
                    var doc = new FreeDocument();
                    doc.Add(Column, currentURL);
                    yield return doc;

                }
                else
                {
                    if (DelayTime > 0)
                    {
                        Thread.Sleep(1000);
                        XLogSys.Print.Debug("empty queue,wait 1s");
                    }
                  
                }
            }
        }
    }
}