using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Input;
using Fiddler;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using HtmlAgilityPack;

namespace Hawk.ETL.Process
{
    [XFrmWork("网页采集器", "自动采集网络脚本", "/XFrmWork.DataMining.Process;component/Images/hadoop.jpg",
        "数据采集和处理")]
    public class SmartCrawler : AbstractProcessMethod, IView
    {
        private readonly Regex extract = new Regex(@"\[(\w+)\]");
        private readonly HttpHelper helper;
        private readonly AutoResetEvent myResetEvent = new AutoResetEvent(false);

        /// <summary>
        ///     当URL发生变化时，自动访问之
        /// </summary>
        public bool AutoVisitUrl = true;

        private IEnumerator<string> currentXPaths;
        public HtmlDocument HtmlDoc = new HtmlDocument();

        /// <summary>
        ///     上一次采集到的数据条目数
        /// </summary>
        public int lastCrawCount;

        private string selectName = "属性0";
        private string selectText = "";
        private string selectXPath = "";
        private string url;
        private string urlHTML;

        public SmartCrawler()
        {
            Http = new HttpItem();
            CrawlItems = new ObservableCollection<CrawlItem>();
            Http.URL = "http://www.cnblogs.com/";

            helper = new HttpHelper();
            IsMultiData = ListType.List;
            Documents = new ObservableCollection<HttpItem>();
        }

        [Browsable(false)]
        public string URLHTML
        {
            get { return urlHTML; }
            set
            {
                if (urlHTML != value)
                {
                    urlHTML = value;
                    OnPropertyChanged("URLHTML");
                }
            }
        }

        [Browsable(false)]
        public string URL
        {
            get { return url; }
            set
            {
                if (url == value) return;
                url = value;
                OnPropertyChanged("URL");
                if (AutoVisitUrl)
                    VisitURLAsync();
            }
        }

        [Browsable(false)]
        public ReadOnlyCollection<ICommand> Commands3
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("刷新网页", obj => VisitURLAsync())
                    });
            }
        }

        [LocalizedDisplayName("执行")]
        [LocalizedCategory("属性提取")]
        [PropertyOrder(6)]
        public ReadOnlyCollection<ICommand> Commands2
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("添加字段", obj => AddNewItem(),
                            obj =>
                                string.IsNullOrEmpty(SelectName) == false && string.IsNullOrEmpty(SelectXPath) == false),
                        new Command("搜索XPath", obj => GetXPathAsync(),
                            obj =>
                                currentXPaths != null),
                        new Command("手气不错",
                            obj => GreatHand(),
                            obj => IsMultiData == ListType.List
                            ),
                        new Command("提取测试", obj =>
                        {
                            if (IsMultiData == ListType.List && string.IsNullOrEmpty(RootXPath))
                            {
                                if (!(CrawlItems.Count != 0).SafeCheck("属性的数量不能为0", LogType.Important))
                                    return;
                                var shortv = HtmlDoc.CompileCrawItems(CrawlItems);
                                if (!string.IsNullOrEmpty(shortv))
                                {
                                    RootXPath = shortv;
                                    OnPropertyChanged("RootXPath");
                                }
                            }


                            var datas = HtmlDoc.GetDataFromXPath(CrawlItems, IsMultiData, RootXPath);
                            var view = PluginProvider.GetObjectInstance<IDataViewer>("可编辑列表");

                            var r = view.SetCurrentView(datas);
                            ControlExtended.DockableManager.AddDockAbleContent(
                                FrmState.Custom, r, "提取数据测试结果");
                        })
                    });
            }
        }

        [LocalizedCategory("属性提取")]
        [LocalizedDisplayName("搜索字符")]
        [PropertyOrder(2)]
        public string SelectText
        {
            get { return selectText; }
            set
            {
                if (selectText == value) return;
                selectText = value;

                currentXPaths = HtmlDoc.SearchXPath(SelectText, () => IsAttribute).GetEnumerator();
                GetXPathAsync();
                OnPropertyChanged("SelectText");
            }
        }

        [LocalizedCategory("属性提取")]
        [PropertyOrder(4)]
        [LocalizedDisplayName("获取的XPath")]
        public string SelectXPath
        {
            get { return selectXPath; }
            set
            {
                if (selectXPath != value)
                {
                    selectXPath = value;
                    OnPropertyChanged("SelectXPath");
                }
            }
        }

        [LocalizedCategory("属性提取")]
        [PropertyOrder(5)]
        [LocalizedDisplayName("属性名称")]
        [LocalizedDescription("为当前属性命名")]
        public string SelectName
        {
            get { return selectName; }
            set
            {
                if (selectName == value) return;
                selectName = value;
                OnPropertyChanged("SelectName");
            }
        }

        [LocalizedCategory("属性提取")]
        [LocalizedDisplayName("读取模式")]
        [LocalizedDescription("当需要获取列表时，选择List,否则选择One")]
        [PropertyOrder(1)]
        public ListType IsMultiData { get; set; }

        [PropertyOrder(6)]
        [LocalizedCategory("属性提取")]
        [LocalizedDisplayName("已有属性")]
        public ObservableCollection<CrawlItem> CrawlItems { get; set; }

        [LocalizedCategory("属性提取")]
        [LocalizedDisplayName("请求详情")]
        [PropertyOrder(11)]
        [LocalizedDescription("设置Cookie和其他访问选项")]
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public HttpItem Http { get; set; }

        [LocalizedDisplayName("提取标签")]
        [PropertyOrder(4)]
        [LocalizedCategory("属性提取")]
        public bool IsAttribute { get; set; }

        [LocalizedCategory("属性提取")]
        [LocalizedDisplayName("父节点Path")]
        [PropertyOrder(8)]
        public string RootXPath { get; set; }

        [Browsable(false)]
        public bool IsRunning { get; private set; }

        [LocalizedCategory("自动嗅探")]
        [LocalizedDisplayName("URL筛选")]
        [LocalizedDescription("仅当请求的URL包含该字段时才会收集信息，使用空格分割，可包含多个关键字段")]
        [PropertyOrder(16)]
        public string URLFilter { get; set; }

        [LocalizedCategory("自动嗅探")]
        [LocalizedDisplayName("内容筛选")]
        [LocalizedDescription("仅当返回的数据包含该字段时才会收集信息，使用空格分割，可包含多个关键字段")]
        [PropertyOrder(17)]
        public string ContentFilter { get; set; }

        [LocalizedCategory("自动嗅探")]
        [LocalizedDisplayName("保存请求")]
        [Browsable(false)]
        [PropertyOrder(18)]
        public bool CanSave { get; set; }

        [Browsable(false)]
        //   [LocalizedCategory("自动嗅探")]
        //  [LocalizedDisplayName("嗅探信息")]
        [PropertyOrder(19)]
        public ObservableCollection<HttpItem> Documents { get; set; }

        [LocalizedCategory("自动嗅探")]
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
                        new Command("开始", obj => StartVisit(), obj => IsRunning == false),
                        new Command("停止", obj => StopVisit(), obj => IsRunning)
                        //     new Command("模拟登录", obj => { AutoVisit(); })
                    });
            }
        }

        [LocalizedCategory("高级设置")]
        [LocalizedDisplayName("共用采集器名")]
        public string Crawler { get; set; }


      

        [Browsable(false)]
        public object UserControl => null;

        [Browsable(false)]
        public FrmState FrmState => FrmState.Large;

        private async void GetXPathAsync()
        {
            if (currentXPaths == null)
                return;
            var r = await MainFrm.RunBusyWork(() => currentXPaths.MoveNext(), "正在查询XPath");
            if (r)
                SelectXPath = currentXPaths.Current;
            else
            {
                XLogSys.Print.Warn("找不到其他符合条件的节点，搜索器已经返回开头");
                currentXPaths = HtmlDoc.SearchXPath(SelectText, () => IsAttribute).GetEnumerator();
            }
        }

        //public void AutoVisit()
        //{
        //    if (Documents.Any())
        //    {
        //        var item = new HttpItem();
        //        Documents[0].DictCopyTo(item);
        //        var res = helper != null && helper.AutoVisit(item);
        //        XLogSys.Print.Info("成功模拟登录");
        //        Http.SetValue("Cookie", item.GetValue("Cookie"));
        //        if (res)
        //        {
        //            URL = item.URL;
        //        }
        //    }
        //}

        private void GreatHand()
        {
            var crawitems = HtmlDoc.SearchPropertiesSmart(CrawlItems, IsAttribute).FirstOrDefault();
            if ((crawitems != null).SafeCheck("网页属性获取", LogType.Info) == false)
                return;


            var datas = HtmlDoc.GetDataFromXPath(crawitems, IsMultiData);

            var propertyNames = new FreeDocument(datas.GetKeys().ToDictionary(d => d, d => (object) d));
            datas.Insert(0, propertyNames);
            var view = PluginProvider.GetObjectInstance<IDataViewer>("可编辑列表");
            var r = view.SetCurrentView(datas);


            var name = "手气不错_可修改第一列的属性名称";
            var window = new Window {Title = name};
            window.Content = r;
            window.Closing += (s, e) =>
            {
                if (ControlExtended.UserCheck("是否确认选择当前的数据表") == false)
                    return;

                foreach (var propertyName in propertyNames)
                {
                    var item = crawitems.FirstOrDefault(d => d.Name == propertyName.Key);
                    if (item == null)
                        continue;
                    if (propertyName.Value == null)
                        continue;
                    item.Name = propertyName.Value.ToString();
                }
                CrawlItems.Clear();
                CrawlItems.AddRange(crawitems);
            };


            window.ShowDialog();
        }

        public override bool Close()
        {
            if (IsRunning)
            {
                StopVisit();
            }
            return base.Close();
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize(scenario);
            dict.Add("URL", URL);
            dict.Add("RootXPath", RootXPath);
            dict.Add("IsMultiData", IsMultiData);
            dict.Add("HttpSet", Http.DictSerialize());
            dict.Add("URLFilter", URLFilter);
            dict.Add("ContentFilter", ContentFilter);
            dict.Add("Crawler", Crawler);
            dict.Children = new List<FreeDocument>();
            if (Documents.Any())
                dict.Add("Login", Documents[0].DictSerialize());
            dict.Children.AddRange(CrawlItems.Select(d => d.DictSerialize(scenario)));
            return dict;
        }

        public void StopVisit()
        {
            FiddlerApplication.AfterSessionComplete -= FiddlerApplicationAfterSessionComplete;
            FiddlerApplication.Shutdown();
            if (CanSave && Documents.Count > 0)
            {
                SysDataManager.AddDataCollection(Documents.Select(d => d.DictSerialize(Scenario.Other)), "爬虫记录");
            }
            IsRunning = FiddlerApplication.IsStarted();
            OnPropertyChanged("IsRunning");
        }

        public void StartVisit()
        {
            if (IsRunning)
                return;
            if (string.IsNullOrEmpty(URLFilter) && string.IsNullOrEmpty(ContentFilter))
            {
                MessageBox.Show("请填写至少填写URL前缀或关键字中一项过滤规则");
                return;
            }
            ControlExtended.SafeInvoke(() =>
            {
                if (CanSave)
                {
                    Documents.Clear();
                }
                var url = URL;
                if (url.StartsWith("http") == false)
                    url = "http://" + url;

                System.Diagnostics.Process.Start(url);
                FiddlerApplication.BeforeResponse += FiddlerApplicationAfterSessionComplete;
                FiddlerApplication.Startup(8888, FiddlerCoreStartupFlags.Default);
                IsRunning = FiddlerApplication.IsStarted();
                OnPropertyChanged("IsRunning");
            }, LogType.Important, "尝试启动服务");
        }

        private void FiddlerApplicationAfterSessionComplete(Session oSession)
        {
            if (string.IsNullOrEmpty(URLFilter) == false)
            {
                URLFilter = URLFilter.Replace("http://", "");
                if (URLFilter.Split(' ').Any(item => oSession.url.Contains(item) == false))
                {
                    return;
                }
            }

            var httpitem = new HttpItem {Parameters = oSession.oRequest.headers.ToString()};


            if ((oSession.BitFlags & SessionFlags.IsHTTPS) != 0)
            {
                httpitem.URL = "https://" + oSession.url;
            }
            else
            {
                httpitem.URL = "http://" + oSession.url;
            }


            httpitem.Postdata = Encoding.Default.GetString(oSession.RequestBody);
            if (string.IsNullOrEmpty(httpitem.Postdata) == false)
            {
                httpitem.Method = MethodType.POST;
                ControlExtended.UIInvoke(() => Documents.Add(httpitem));
            }


            if (string.IsNullOrEmpty(ContentFilter) == false)
            {
                if (ContentFilter.Split(' ').Any(item => oSession.GetResponseBodyAsString().Contains(item) == false))
                {
                    return;
                }
            }


            httpitem.DictCopyTo(Http);
            XLogSys.Print.Info("已经成功获取嗅探字段" + oSession.url);
        }

        public override void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(dicts, scenario);
            URL = dicts.Set("URL", URL);
            RootXPath = dicts.Set("RootXPath", RootXPath);
            IsMultiData = dicts.Set("IsMultiData", IsMultiData);
            URLFilter = dicts.Set("URLFilter", URLFilter);
            Crawler = dicts.Set("Crawler", Crawler);
            ContentFilter = dicts.Set("ContentFilter", ContentFilter);
            if (dicts.ContainsKey("HttpSet"))
            {
                var doc2 = dicts["HttpSet"];
                var p = doc2 as IDictionary<string, object>;
                Http.UnsafeDictDeserialize(p);
            }

            if (dicts.ContainsKey("Login"))
            {
                var doc2 = dicts["Login"];
                var p = doc2 as IDictionary<string, object>;
                var item = new HttpItem();
                item.DictDeserialize(p);
                Documents.Add(item);
            }

            if (dicts.ContainsKey("Generator"))
            {
                var doc2 = dicts["Generator"];
                var p = doc2 as IDictionary<string, object>;
            }
            var doc = dicts as FreeDocument;
            if (doc?.Children != null)
            {
                foreach (var child in doc.Children)
                {
                    var item = new CrawlItem();
                    item.DictDeserialize(child);
                    CrawlItems.Add(item);
                }
            }
        }

        private void AddNewItem(bool isAlert = true)
        {
            var path = SelectXPath;
            if (!string.IsNullOrEmpty(RootXPath))
            {
                var root = HtmlDoc.DocumentNode.SelectSingleNode(RootXPath).ParentNode;
                var node = HtmlDoc.DocumentNode.SelectSingleNode(path);
                if (!node.IsAncestor(root))
                {
                    if (isAlert)
                        MessageBox.Show("当前XPath所在节点不是父节点的后代，请检查对应的XPath");
                    return;
                }
                path = new XPath(node.XPath).TakeOff(root.XPath).ToString();
            }

            var item = new CrawlItem {XPath = path, Name = SelectName, SampleData1 = SelectText};
            if (CrawlItems.Any(d => d.Name == SelectName))
            {
                SelectName = "属性" + CrawlItems.Count;
                if (isAlert)
                {
                    MessageBox.Show($"已存在名称为{SelectName}的属性，不能重复添加");
                    return;
                }
            }
            CrawlItems.Add(item);
            SelectXPath = "";
        }

        public List<FreeDocument> CrawlData(HtmlDocument doc)
        {
            if (CrawlItems.Count == 0)
            {
                var freedoc = new FreeDocument();
                freedoc.Add("Content", doc.DocumentNode.OuterHtml);
                
                return new List<FreeDocument> {freedoc};
            }
            return doc.GetDataFromXPath(CrawlItems, IsMultiData, RootXPath);
        }

        public List<FreeDocument> CrawlData(string url, out HtmlDocument doc, out HttpStatusCode code,
            string post = null)
        {
            var mc = extract.Matches(url);
            Dictionary<string, string> paradict = null;
            foreach (Match m in mc)
            {
                if (paradict == null)
                    paradict = XPathAnalyzer.ParseUrl(URL);
                if (paradict == null)
                    break;
                var str = m.Groups[1].Value;
                if (paradict.ContainsKey(str))
                {
                    url = url.Replace(m.Groups[0].Value, paradict[str]);
                }
            }
            if (!string.IsNullOrEmpty(Crawler))
            {
                var crawler =
                    SysProcessManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == Crawler) as SmartCrawler;
                var header = crawler?.Http.GetHeaderParameter();

                if (header != null)
                {
                    var myheader = Http.GetHeaderParameter();
                    object value;

                    if (header.TryGetValue("Cookie", out value))
                    {
                        myheader["Cookie"]= value.ToString();
                    }
                    if (header.TryGetValue("Host", out value))
                    {
                        myheader["Host"] = value.ToString();
                    }
                    if (header.TryGetValue("Referer", out value))
                    {
                        myheader["Referer"] = value.ToString();
                    }
                    Http.Parameters = HttpItem.HeaderToString(myheader);
                }

            }
        
            var content = helper.GetHtml(Http, out code, url, post);
            doc = new HtmlDocument();
            if (!HttpHelper.IsSuccess(code))
            {
                XLogSys.Print.WarnFormat("HTML Fail,Code:{0}，url:{1}", code, url);
                return new List<FreeDocument>();
            }


            doc.LoadHtml(content);
            var datas = CrawlData(doc);
            if (datas.Count == 0)
            {
                XLogSys.Print.DebugFormat("HTML extract Fail，url:{0}", url);
            }
           
            return datas;
        }

        private async void VisitURLAsync()
        {
            URLHTML = await MainFrm.RunBusyWork(() =>
            {
                HttpStatusCode code;
                var item2 = helper.GetHtml(Http, out code, URL);
                return item2;
            });
            HtmlDoc.LoadHtml(URLHTML);
            OnPropertyChanged("URLHTML");
        }
    }
}