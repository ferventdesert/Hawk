using System;
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

        /// <summary>
        ///     当URL发生变化时，自动访问之
        /// </summary>
        public bool AutoVisitUrl = true;


        private XPathAnalyzer.CrawTarget CrawTarget;
        private IEnumerator<string> currentXPaths;
        public bool enableRefresh = true;
        public HtmlDocument HtmlDoc = new HtmlDocument();

        /// <summary>
        ///     上一次采集到的数据条目数
        /// </summary>
        public int lastCrawCount;

        private string selectName = "属性0";
        private string selectText = "";
        private string selectXPath = "";
        private string url = "";
        private string urlHTML = "";
        private int xpath_count;
        private bool _isSuperMode;

        public SmartCrawler()
        {
            Http = new HttpItem();
            CrawlItems = new ObservableCollection<CrawlItem>();
            helper = new HttpHelper();
            //  URL = "https://detail.tmall.com/item.htm?spm=a230r.1.14.3.jJHewQ&id=525379044994&cm_id=140105335569ed55e27b&abbucket=16&skuId=3126989942045";
            // SelectText = "自带投影自带音箱不发音";
            URL = "";
            SelectText = "";
            IsMultiData = ListType.List;
            IsAttribute = true;
            IsSuperMode = true;
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
                    VisitUrlAsync();
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
                        new Command("刷新网页", obj => VisitUrlAsync()),
                        new Command("复制到剪切板", obj => CopytoClipBoard())
                    });
            }
        }

        [LocalizedDisplayName("执行")]
        [LocalizedCategory("2.属性提取")]
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
                            if (!(CrawlItems.Count > 0).SafeCheck("属性数量不能为空"))
                                return;

                            if (IsMultiData == ListType.List && CrawlItems.Count < 2)
                            {
                                MessageBox.Show("列表模式下，属性数量不能少于2个", "提示信息");
                                return;
                            }
                            var datas = HtmlDoc.GetDataFromXPath(CrawlItems, IsMultiData, RootXPath);
                            var view = PluginProvider.GetObjectInstance<IDataViewer>("可编辑列表");

                            var r = view.SetCurrentView(datas);
                            ControlExtended.DockableManager.AddDockAbleContent(
                                FrmState.Custom, r, "提取数据测试结果");

                            var rootPath =
                                XPath.GetMaxCompareXPath(CrawlItems.Select(d => d.XPath));
                            if (datas.Count > 1 && string.IsNullOrEmpty(RootXPath) && rootPath.Length > 0 &&
                                IsMultiData == ListType.List &&
                                MessageBox.Show($"检测到列表的根节点为:{rootPath}，是否设置根节点路径？ 此操作有建议有经验用户使用，小白用户请点【否】", "提示信息",
                                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                RootXPath = rootPath;
                                HtmlDoc.CompileCrawItems(CrawlItems);
                                OnPropertyChanged("RootXPath");
                            }
                        })
                    });
            }
        }

        [LocalizedCategory("2.属性提取")]
        [LocalizedDisplayName("搜索字符")]
        [PropertyOrder(2)]
        public string SelectText
        {
            get { return selectText; }
            set
            {
                if (selectText == value) return;
                selectText = value;
                xpath_count = 0;
                if (string.IsNullOrWhiteSpace(selectText) == false)
                {
                    currentXPaths = HtmlDoc.SearchXPath(SelectText, () => IsAttribute).GetEnumerator();
                    GetXPathAsync();
                }
                OnPropertyChanged("SelectText");
            }
        }

        [LocalizedCategory("2.属性提取")]
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

        [LocalizedCategory("2.属性提取")]
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
                if (string.IsNullOrEmpty(this.SelectName))
                {
                    SelectName = "属性"+CrawlItems.Count;
                }
            }
        }

        [LocalizedCategory("2.属性提取")]
        [LocalizedDisplayName("读取模式")]
        [LocalizedDescription("当需要获取列表时，选择List,否则选择One")]
        [PropertyOrder(1)]
        public ListType IsMultiData { get; set; }

        [PropertyOrder(6)]
        [LocalizedCategory("2.属性提取")]
        [LocalizedDisplayName("已有属性")]
        public ObservableCollection<CrawlItem> CrawlItems { get; set; }

        [LocalizedCategory("2.属性提取")]
        [LocalizedDisplayName("请求详情")]
        [PropertyOrder(11)]
        [LocalizedDescription("设置Cookie和其他访问选项")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public HttpItem Http { get; set; }

        [Browsable(false)]
        [LocalizedDisplayName("提取标签")]
        [PropertyOrder(4)]
        [LocalizedCategory("2.属性提取")]
        public bool IsAttribute { get; set; }

        [LocalizedCategory("2.属性提取")]
        [LocalizedDescription("当设置该值后，所有属性Path都应该为父节点的子path，而不能是完整的xpath路径")]
        [LocalizedDisplayName("父节点Path")]
        [PropertyOrder(8)]
        public string RootXPath { get; set; }

        [Browsable(false)]
        public bool IsRunning => FiddlerApplication.IsStarted();

        [LocalizedCategory("自动嗅探")]
        [LocalizedDisplayName("保存请求")]
        [Browsable(false)]
        [PropertyOrder(18)]
        public bool CanSave { get; set; }

        [LocalizedCategory("3.动态请求嗅探")]
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

        [LocalizedCategory("3.动态请求嗅探")]
        [LocalizedDisplayName("超级模式")]
        [LocalizedDescription("该模式能够强力解析网页内容，但是比较消耗资源，且会修改原始Html内容")]
        [PropertyOrder(9)]
        public bool IsSuperMode
        {
            get { return _isSuperMode; }
            set
            {
                if (_isSuperMode != value)
                {
                    _isSuperMode = value;
                    OnPropertyChanged("IsSuperMode");
                }
            }
        }

        [Browsable(false)]
        public object UserControl => null;

        [Browsable(false)]
        public FrmState FrmState => FrmState.Large;

        private void CopytoClipBoard()
        {
            Clipboard.SetDataObject(URLHTML);
            XLogSys.Print.Info("已经成功复制到剪贴板");
        }

        private async void GetXPathAsync()
        {
            if (currentXPaths == null)
                return;
            xpath_count++;
            try
            {
                var r = await MainFrm.RunBusyWork(() => currentXPaths.MoveNext(), "正在查询XPath");
                if (r)
                    SelectXPath = currentXPaths.Current;
                else
                {
                    SelectXPath = "";
                    if (xpath_count > 1)
                    {
                        XLogSys.Print.Warn("找不到其他符合条件的节点，搜索器已经返回开头");
                        currentXPaths = HtmlDoc.SearchXPath(SelectText, () => IsAttribute).GetEnumerator();
                    }
                    else
                    {
                        XLogSys.Print.Warn($"在该网页中找不到关键字 {SelectText},可能是动态请求，可以启用【自动嗅探】,并将浏览器页面翻到包含该关键字的位置");
                    }
                }
            }
            catch (Exception ex)
            {
                XLogSys.Print.Warn($"查询XPath时在内部发生异常:" +ex.Message);
            }
          
        }

        public void GreatHand()
        {
            var count = 0;
            var crawTargets = HtmlDoc.SearchPropertiesSmart(CrawlItems, RootXPath, IsAttribute);
            var currentCrawTargets = crawTargets.GetEnumerator();
            var result = currentCrawTargets.MoveNext();
            if (result)
                CrawTarget = currentCrawTargets.Current;
            else
            {
                CrawTarget = null;
                XLogSys.Print.Warn("没有检查到任何可选的列表页面");
                return;
            }

            var crawitems = CrawTarget.CrawItems;
            var datas = HtmlDoc.GetDataFromXPath(crawitems, IsMultiData, CrawTarget.RootXPath);
            var propertyNames = new FreeDocument(datas.GetKeys().ToDictionary(d => d, d => (object)d));
            datas.Insert(0, propertyNames);
            var view = PluginProvider.GetObjectInstance<IDataViewer>("可编辑列表");
            var r = view.SetCurrentView(datas);


            var name = "手气不错_可修改第一列的属性名称";
            var window = new Window { Title = name };
            window.Content = r;
            window.Closing += (s, e) =>
            {
                var check = MessageBox.Show("是否确认选择当前结果？【是】：确认结果，  【否】:检查下个目标，  【取消】:结束当前手气不错", "提示信息",
                    MessageBoxButton.YesNoCancel);
                switch (check)
                {
                    case MessageBoxResult.Yes:
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
                        RootXPath = CrawTarget.RootXPath;
                        CrawlItems.AddRange(crawitems);
                        currentCrawTargets = null;
                        break;
                    case MessageBoxResult.No:
                        e.Cancel = true;
                        result = currentCrawTargets.MoveNext();
                        count++;
                        if (result)
                            CrawTarget = currentCrawTargets.Current;
                        else
                        {
                            MessageBox.Show("已经搜索所有可能情况，搜索器已经返回开头");
                            crawTargets = HtmlDoc.SearchPropertiesSmart(CrawlItems, RootXPath, IsAttribute);
                            currentCrawTargets = crawTargets.GetEnumerator();
                            count = 0;
                            result = currentCrawTargets.MoveNext();
                            if (!result)
                            {
                                e.Cancel = false;
                            }
                            else
                            {
                                CrawTarget = currentCrawTargets.Current;
                            }
                        }

                        crawitems = CrawTarget.CrawItems;
                        var title = $"手气不错，第{count}次结果";
                        datas = HtmlDoc.GetDataFromXPath(crawitems, IsMultiData, CrawTarget.RootXPath);
                        propertyNames = new FreeDocument(datas.GetKeys().ToDictionary(d => d, d => (object)d));
                        datas.Insert(0, propertyNames);
                        r = view.SetCurrentView(datas);
                        window.Content = r;
                        window.Title = title; 
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
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
            dict.Add("IsSuperMode", IsSuperMode);
            dict.Add("HttpSet", Http.DictSerialize());
            dict.Children = new List<FreeDocument>();
            dict.Children.AddRange(CrawlItems.Select(d => d.DictSerialize(scenario)));
            return dict;
        }

        public void StopVisit()
        {
            FiddlerApplication.AfterSessionComplete -= FiddlerApplicationAfterSessionComplete;
            FiddlerApplication.Shutdown();
            OnPropertyChanged("IsRunning");
        }

        public void Class1()
        {
            //Fiddler.CONFIG.IgnoreServerCertErrors = false;

            //Fiddlerapplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);
            //FiddlerCoreStartupFlags oFCSF = FiddlerCoreStartupFlags.Default;
            //int iPort = 8877;
            //Fiddler.FiddlerApplication.Startup(iPort, oFCSF);
            //FiddlerApplication.Log.LogFormat("Created endpoint listening on port {0}", iPort);
            //FiddlerApplication.Log.LogFormat("Starting with settings: [{0}]", oFCSF);
            //FiddlerApplication.Log.LogFormat("Gateway: {0}", CONFIG.UpstreamGateway.ToString());
            //oSecureEndpoint = FiddlerApplication.CreateProxyEndpoint(iSecureEndpointPort, true, sSecureEndpointHostname);
            //Proxies.SetProxy("");
            //if (Fiddler.CertMaker.trustRootCert() == true)
            //{
            //    Join("欢迎使用某某软件[具体操作请看说明]");
            //    Join(Form1.Logincfg);
            //}
            //else
            //{
            //    Join("证书安装出错");

            //}
            //Fiddler.FiddlerApplication.OnNotification += delegate (object sender, NotificationEventArgs oNEA)
            //{ Console.WriteLine("** NotifyUser: " + oNEA.NotifyString); };
            ////Fiddler.FiddlerApplication.Log.OnLogString += delegate(object sender, LogEventArgs oLEA)
            //{ Console.WriteLine("** LogString: " + oLEA.LogString); };//记录步骤           
            //Fiddler.FiddlerApplication.BeforeRequest += delegate (Fiddler.Session oS)//客户端请求时，此事件触发   
            //{
            //    oS.bBufferResponse = true;//内容是否更新           
            //    Monitor.Enter(oAllSessions);
            //    oAllSessions.Add(oS);
            //    Monitor.Exit(oAllSessions);
            //    oS["X-AutoAuth"] = "(default)";
            //    if ((oS.oRequest.pipeClient.LocalPort == iSecureEndpointPort) && (oS.hostname == sSecureEndpointHostname))
            //    {
            //        oS.utilCreateResponseAndBypassServer();
            //        oS.oResponse.headers.HTTPResponseStatus = "200 Ok";
            //        oS.oResponse["Content-Type"] = "text/html; charset=UTF-8"; oS.oResponse["Cache-Control"] = "private, max-age=0"; oS.utilSetResponseBody("<html><body>Request for httpS://" + sSecureEndpointHostname + ":" + iSecureEndpointPort.ToString() + " received. Your request was:<br /><plaintext>" + oS.oRequest.headers.ToString());
            //    }
            //}; Fiddler.FiddlerApplication.BeforeResponse += delegate (Fiddler.Session oS) //接受到会话时触发      
            //{
            ////这边为主要修改地点 //oS  通过调用oS这个类型来实现  修改任意数据  链接 cookie  body  返回内容等等  只要你想得到  都能实现 }
            //Fiddler.FiddlerApplication.AfterSessionComplete += delegate(Fiddler.Session oS)
            ////这在一个会话已完成事件触发          
            //{
            //    //清理创建的任何临时文件|M:Fiddler.FiddlerApplication.WipeLeakedFiles   我要中这个函数 可是怎么用都说没引用？？         
            //    //oS.ResponseBody             
            //    //Console.WriteLine("输出测试：" + Fiddler.ServerChatter.ParseResponseForHeaders);//返回文本内容   
            //    //Console.WriteLine("Finished session:\t" + oS.fullUrl); //获取连接URL            
            //    //Console.Title = ("Session list contains: " + oAllSessions.Count.ToString() + " sessions");         
            //    //oS.PathAndQuery 获取最后页面路径  /1.htm                   //oS.RequestMethod 获取方法 GET 等等         
            //};             Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
        }

        public void StartVisit()
        {
            if (IsRunning)
                return;
            if (string.IsNullOrWhiteSpace(SelectText))
            {
                MessageBox.Show("请填写包含在页面中的关键字信息：【2.属性提取】->【搜索字符】");
                return;
            }
            ControlExtended.SafeInvoke(() =>
            {
                var url = URL;
                if (url.StartsWith("http") == false)
                    url = "http://" + url;


                CONFIG.bMITM_HTTPS = true;
                FiddlerApplication.AfterSessionComplete += FiddlerApplicationAfterSessionComplete;

                FiddlerApplication.Startup(8888, true, true);
                System.Diagnostics.Process.Start(url);
                OnPropertyChanged("IsRunning");
            }, LogType.Important, "启动嗅探服务");
        }

        private void FiddlerApplicationAfterSessionComplete(Session oSession)
        {
            if (oSession.oRequest.headers == null)
                return;
            var httpitem = new HttpItem { Parameters = oSession.oRequest.headers.ToString() };


            if ((oSession.BitFlags & SessionFlags.IsHTTPS) != 0)
            {
                httpitem.URL = "https://" + oSession.url;
            }
            else
            {
                httpitem.URL = "http://" + oSession.url;
            }


            httpitem.Postdata = Encoding.Default.GetString(oSession.RequestBody);


            if (string.IsNullOrWhiteSpace(SelectText) == false)
            {
                var content = oSession.GetResponseBodyAsString();

                content = JavaScriptAnalyzer.Decode(content);
                if (content.Contains(SelectText) == false)
                {
                    return;
                }
            }
            IsSuperMode = true;
            StopVisit();
            httpitem.DictCopyTo(Http);
            var post = "";
            if (Http.Method == MethodType.POST)
            {
                post = "post请求的内容为:\n" + httpitem.Postdata + "\n";
            }
            var window = MainFrm as Window;
            ControlExtended.UIInvoke(() => { if (window != null) window.Topmost = true; });
            var info = $"已经成功获取嗅探字段！ 真实请求地址:\n{oSession.url}，\n已自动配置了网页采集器，请求类型为{Http.Method}\n {post}已经刷新了网页采集器的内容";
            XLogSys.Print.Info(info);
            ControlExtended.UIInvoke(() => { if (window != null) window.Topmost = false; });
            URL = oSession.url;

        }

        public override void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(dicts, scenario);
            URL = dicts.Set("URL", URL);
            RootXPath = dicts.Set("RootXPath", RootXPath);
            IsMultiData = dicts.Set("IsMultiData", IsMultiData);
            IsSuperMode = dicts.Set("IsSuperMode", IsSuperMode);
            if (dicts.ContainsKey("HttpSet"))
            {
                var doc2 = dicts["HttpSet"];
                var p = doc2 as IDictionary<string, object>;
                Http.UnsafeDictDeserialize(p);
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
                //TODO: 当XPath路径错误时，需要捕获异常
                HtmlNode root = null;
                try
                {
                    root = HtmlDoc.DocumentNode.SelectSingleNode(RootXPath);
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error($"{RootXPath}  不能被识别为正确的XPath表达式，请检查");
                }
                if (!(root != null).SafeCheck("使用当前父节点XPath，在文档中找不到任何父节点"))
                    return;
                root = HtmlDoc.DocumentNode.SelectSingleNode(RootXPath)?.ParentNode;

                HtmlNode node = null;
                if (
                    !ControlExtended.SafeInvoke(() => HtmlDoc.DocumentNode.SelectSingleNode(path), ref node,
                        LogType.Info, "检查子节点XPath正确性", true))

                {
                    return;
                }
                if (!(node != null).SafeCheck("使用当前子节点XPath，在文档中找不到任何子节点"))
                    return;
                if (!node.IsAncestor(root))
                {
                    if (isAlert)
                        if (
                            MessageBox.Show("当前XPath所在节点不是父节点的后代，请检查对应的XPath，是否依然要添加?", "提示信息", MessageBoxButton.YesNo) ==
                            MessageBoxResult.Yes)
                        {
                            path = XPath.TakeOff(node.XPath, root.XPath);
                        }
                        else
                        {
                            return;
                        }
                }
            }

            var item = new CrawlItem { XPath = path, Name = SelectName, SampleData1 = SelectText };
            CrawlItems.Add(item);
            SelectXPath = "";
            SelectName = "";
        }

        public List<FreeDocument> CrawlData(HtmlDocument doc)
        {
            if (CrawlItems.Count == 0)
            {
                var freedoc = new FreeDocument { { "Content", doc.DocumentNode.OuterHtml } };

                return new List<FreeDocument> { freedoc };
            }
            return doc.GetDataFromXPath(CrawlItems, IsMultiData, RootXPath);
        }

        public string GetHtml(string url, out HttpStatusCode code,
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
            WebHeaderCollection headerCollection;
            var content = helper.GetHtml(Http, out headerCollection, out code, url, post);
            content = JavaScriptAnalyzer.Decode(content);
            if (IsSuperMode)
            {

                content = JavaScriptAnalyzer.Parse2XML(content);
            }

            return content;
        }

        public List<FreeDocument> CrawlData(string url, out HtmlDocument doc, out HttpStatusCode code,
            string post = null)
        {
            var content = GetHtml(url, out code, post);


            return CrawlHtmlData(content, out doc);
        }

        public List<FreeDocument> CrawlHtmlData(string html, out HtmlDocument doc
            )
        {
            if (IsSuperMode)
            {
                html = JavaScriptAnalyzer.Parse2XML(html);
            }
            doc = new HtmlDocument();

            doc.LoadHtml(html);
            var datas = new List<FreeDocument>();
            try
            {
                datas = CrawlData(doc);
                if (datas.Count == 0)
                {
                    XLogSys.Print.InfoFormat("HTML抽取数据失败，url:{0}", url);
                }
            }
            catch (Exception ex)
            {
                XLogSys.Print.ErrorFormat("HTML抽取数据失败，url:{0}, 异常为{1}", url, ex.Message);
            }


            return datas;
        }

        private async void VisitUrlAsync()
        {
            if (!enableRefresh)
                return;
            URLHTML = await MainFrm.RunBusyWork(() =>
            {
                HttpStatusCode code;
                return GetHtml(URL, out code);
            });
            if (URLHTML.Contains("尝试自动重定向") &&
                MessageBox.Show("网站提示: " + URLHTML + "\n 通常原因是网站对请求合法性做了检查, 建议填写关键字对网页内容进行自动嗅探", "提示信息",
                    MessageBoxButton.OK) == MessageBoxResult.OK)

            {
                return;
            }


            ControlExtended.SafeInvoke(() => HtmlDoc.LoadHtml(URLHTML), name: "解析html文档");


            if (string.IsNullOrWhiteSpace(selectText) == false)
            {
                currentXPaths = HtmlDoc.SearchXPath(SelectText, () => IsAttribute).GetEnumerator();
                GetXPathAsync();
            }
            OnPropertyChanged("URLHTML");
        }
    }
}