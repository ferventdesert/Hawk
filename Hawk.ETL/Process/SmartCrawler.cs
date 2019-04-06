using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using System.Windows.Input;
using Fiddler;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Plugins.Transformers;
using HtmlAgilityPack;
using IronPython.Runtime.Operations;
using ScrapySharp.Network;

namespace Hawk.ETL.Process
{
    [XFrmWork("smartcrawler_name", "SmartCrawler_desc", "camera","数据采集和处理")]
    public class SmartCrawler : AbstractProcessMethod, IView
    {
        /// <summary>
        ///     当URL发生变化时，自动访问之
        /// </summary>
        public static bool AutoVisitUrl = true;

        private readonly Regex extract = new Regex(@"\[(\w+)\]");
        private readonly HttpHelper helper;
        private bool _isSuperMode;
        private SelectorFormat _rootFormat;
        private string _rootXPath;
        private SelectorFormat _searchFormat;
        private ScrapingBrowser browser = new ScrapingBrowser();
        private IEnumerator<string> currentXPaths;
        public bool enableRefresh = true;
        private bool hasInit;

        public HtmlDocument HtmlDoc { get; set; }
        private TextBox htmlTextBox;
        private bool isBusy;

        /// <summary>
        ///     上一次采集到的数据条目数
        /// </summary>
        public int lastCrawCount;

        private string selectName = GlobalHelper.Get("key_621");
        private string selectText = "";
        private string selectXPath = "";
        private string url = "";
        private string urlHTML = "";
        private int xpath_count;

        public SmartCrawler()
        {
            Http = new HttpItem();
            CrawlItems = new ObservableCollection<CrawlItem>();
            helper = new HttpHelper();
            URL = "";
            HtmlDoc=new HtmlDocument();
            SelectText = "";
            IsMultiData = ScriptWorkMode.List;
            IsAttribute = true;
            URL = "www.cnblogs.com";
            ShareCookie = new TextEditSelector();
            ShareCookie.GetItems = AppHelper.GetAllCrawlerNames(null);
            Commands2 = CommandBuilder.GetCommands(
                this,
                new[]
                {
                    new Command(GlobalHelper.Get("key_302"), obj => AddNewItem(),
                        obj =>
                            string.IsNullOrEmpty(SelectName) == false && string.IsNullOrEmpty(SelectXPath) == false,
                        "add"),
                    new Command(GlobalHelper.Get("search"), obj => GetXPathAsync(),
                        obj =>
                            currentXPaths != null, "magnify"),
                    new Command(GlobalHelper.Get("feellucky"),
                        obj => FeelLucky(),
                        obj => IsMultiData != ScriptWorkMode.NoTransform && isBusy == false, "smiley_happy"
                        ),
                    new Command(GlobalHelper.Get("key_624"), obj =>
                    {
                        if (!(CrawlItems.Count > 0).SafeCheck(GlobalHelper.Get("key_625")))
                            return;

                        if (IsMultiData == ScriptWorkMode.List && CrawlItems.Count < 2)
                        {
                            MessageBox.Show(GlobalHelper.Get("key_626"), GlobalHelper.Get("key_99"));
                            return;
                        }
                        if (string.IsNullOrEmpty(this.URLHTML))
                        {
                            this.VisitUrlAsync();
                        }

                        var datas =
                            HtmlDoc.DocumentNode.GetDataFromXPath(CrawlItems, IsMultiData, RootXPath, RootFormat).Take(20)
                                .ToList();
                        var view = PluginProvider.GetObjectInstance<IDataViewer>(GlobalHelper.Get("key_230"));

                        var r = view.SetCurrentView(datas);
                        ControlExtended.DockableManager.AddDockAbleContent(
                            FrmState.Custom, r, GlobalHelper.Get("key_627"));

                        var rootPath =
                            XPath.GetMaxCompareXPath(CrawlItems.Select(d => d.XPath));
                        if (datas.Count > 1 && string.IsNullOrEmpty(RootXPath) && rootPath.Length > 0 &&
                            IsMultiData == ScriptWorkMode.List &&
                            MessageBox.Show(string.Format(GlobalHelper.Get("key_628"),rootPath), GlobalHelper.Get("key_99"),
                                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            RootXPath = rootPath;
                            RootFormat = SelectorFormat.XPath;
                            HtmlDoc.CompileCrawItems(CrawlItems);
                            OnPropertyChanged("RootXPath");
                        }
                    }, icon: "page_search")
                });
        }

        [Browsable(false)]
        public SelectorFormat RootFormat
        {
            get { return _rootFormat; }
            set
            {
                if (_rootFormat != value)
                {
                    _rootFormat = value;
                    OnPropertyChanged("RootFormat");
                }
            }
        }

        [Browsable(false)]
        public SelectorFormat SearchFormat
        {
            get { return _searchFormat; }
            set
            {
                if (_searchFormat != value)
                {
                    _searchFormat = value;
                    OnPropertyChanged("SearchFormat");
                }
            }
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

        [PropertyEditor("CodeEditor")]
        [PropertyOrder(100)]
        [LocalizedDisplayName("remark")]
        [LocalizedDescription("remark_desc")]
        public string Remark { get; set; }





        [Browsable(false)]
        public ReadOnlyCollection<ICommand> Commands3
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_629"), obj => VisitUrlAsync(), icon: "refresh"),
                        new Command(GlobalHelper.Get("key_630"), obj => CopytoClipBoard(), icon: "clipboard_file"),
                        new Command(GlobalHelper.Get("key_631"), obj => EditProperty(), icon: "edit"),
                        new Command(GlobalHelper.Get("key_632"), obj =>
                        {
                            if (ControlExtended.UserCheck(GlobalHelper.Get("key_633")))
                            {
                                this.CrawlItems.Clear();
                            }
                        }, obj=>this.CrawlItems.Count>0, icon: "edit")
                    });
            }
        }

        [Browsable(false)]
        public ReadOnlyCollection<ICommand> Commands2 { get; }

        [Browsable(false)]
        public string SelectText
        {
            get { return selectText; }
            set
            {
                if (selectText == value) return;
                selectText = value;
                (Commands2[1] as Command).Text = GlobalHelper.Get("search");
                xpath_count = 0;
                if (string.IsNullOrWhiteSpace(selectText) == false)
                {
                    currentXPaths = HtmlDoc.SearchXPath(SelectText, () => IsAttribute).GetEnumerator();
                    GetXPathAsync();
                }
                OnPropertyChanged("SelectText");
            }
        }

        [Browsable(false)]
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

        [Browsable(false)]
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

        [Browsable(false)]
        public ScriptWorkMode IsMultiData { get; set; }

        [Browsable(false)]
        public ObservableCollection<CrawlItem> CrawlItems { get; set; }

        [LocalizedCategory("key_634")]
        [LocalizedDisplayName("http_header")]
        [PropertyOrder(1)]
        [LocalizedDescription("key_636")]
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public HttpItem Http { get; set; }

        [Browsable(false)]
        [LocalizedDisplayName("key_637")]
        [PropertyOrder(4)]
        [LocalizedCategory("key_638")]
        public bool IsAttribute { get; set; }

        [Browsable(false)]
        public string RootXPath
        {
            get { return _rootXPath; }
            set
            {
                if (_rootXPath != value)
                {
                    _rootXPath = value;
                    OnPropertyChanged("RootXPath");
                }
            }
        }

        [Browsable(false)]
        public bool IsRunning => FiddlerApplication.IsStarted();

        [LocalizedCategory("key_639")]
        [LocalizedDisplayName("key_640")]
        [Browsable(false)]
        [PropertyOrder(18)]
        public bool CanSave { get; set; }

        [LocalizedCategory("key_641")]
        [LocalizedDisplayName("key_34")]
        [PropertyOrder(20)]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_642"), obj => StartVisit(), obj => IsRunning == false, "camera"),
                        new Command(GlobalHelper.Get("key_643"), obj => StopVisit(), obj => IsRunning, "stop"),
                         new Command(GlobalHelper.Get("key_644"), obj => ConfigParam(), obj => true, "edit")
                        //     new Command("模拟登录", obj => { AutoVisit(); })
                        //     new Command("模拟登录", obj => { AutoVisit(); })
                    });
            }
        }

        private void ConfigParam()
        {
            PropertyGridFactory.GetPropertyWindow(this.Http).ShowDialog();

        }



        [LocalizedCategory("key_199")]
        [LocalizedDisplayName("key_200")]
        public override string Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;


                if (this.hasInit&&MainDescription.IsUIForm && string.IsNullOrEmpty(_name) == false &&
                    string.IsNullOrEmpty(value) == false)
                {
                    var dock = MainFrm as IDockableManager;
                    var view = dock?.ViewDictionary.FirstOrDefault(d => d.Model == this);
                    if (view != null)
                    {
                        dynamic container = view.Container;
                        container.Title = value;
                    }
                    var oldCrawler = SysProcessManager.CurrentProcessCollections.OfType<SmartCrawler>()
                        .Where(d => d.ShareCookie.SelectItem == _name).ToList();
                    var oldEtls = SysProcessManager.CurrentProcessCollections.OfType<SmartETLTool>()
                        .SelectMany(d => d.CurrentETLTools).OfType<ResponseTF>()
                        .Where(d => d.CrawlerSelector.SelectItem == _name).ToList();

                    if ((oldCrawler.Count>0|| oldEtls.Count>0)&& MessageBox.Show(string.Format(GlobalHelper.Get("check_if_rename"), this.TypeName, _name, value,
                        string.Join(",", oldCrawler.Select(d => d.Name)), string.Join(",", oldEtls.Select(d => d.ObjectID))), GlobalHelper.Get("Tips"),MessageBoxButton.YesNo)==MessageBoxResult.Yes)
                    {
                        oldCrawler.Execute(d => d.ShareCookie.SelectItem = value);
                        oldEtls.Execute(d=>d.CrawlerSelector.SelectItem=value);
                    }
                }
                _name = value;

                OnPropertyChanged("Name");

            }
        }



        [LocalizedCategory("key_634")]
        [LocalizedDisplayName("key_645")]
       
        [PropertyOrder(9)]
        public bool IsSuperMode
        {
            get { return _isSuperMode; }
            set
            {
                if (_isSuperMode != value)
                {
                    _isSuperMode = value;
                    if (AutoVisitUrl)
                    {
                        VisitUrlAsync();
                    }

                    OnPropertyChanged("IsSuperMode");
                }
            }
        }

        [LocalizedCategory("key_634")]
        [LocalizedDisplayName("key_647")]
        [LocalizedDescription("key_648")]
        public TextEditSelector ShareCookie { get; set; }

        [Browsable(false)]
        public object UserControl => null;

        [Browsable(false)]
        public FrmState FrmState => FrmState.Large;

        private void CopytoClipBoard()
        {
            Clipboard.SetDataObject(URLHTML);
            XLogSys.Print.Info(GlobalHelper.Get("key_649"));
        }

        private async void GetXPathAsync()
        {
            if (currentXPaths == null)
                return;
            xpath_count++;
            try
            {
                var r = await MainFrm.RunBusyWork(() => currentXPaths.MoveNext(), GlobalHelper.Get("key_650"));
                if (r)
                {
                    SelectXPath = currentXPaths.Current;
                    SearchFormat = SelectorFormat.XPath;
                    var node = HtmlDoc.DocumentNode.SelectSingleNodePlus(SelectXPath, SelectorFormat.XPath);
                    if (node != null && MainDescription.IsUIForm)
                    {
                        if (htmlTextBox == null)
                        {
                            var dock = MainFrm as IDockableManager ?? ControlExtended.DockableManager;
                            var control = dock?.ViewDictionary.FirstOrDefault(d => d.Model == this);
                            if (control != null)
                            {
                                dynamic dy = control.View;
                                htmlTextBox = dy.HTMLTextBox;
                            }
                        }
                        if (htmlTextBox != null)
                        {
                            ControlExtended.UIInvoke(() =>
                            {
                                htmlTextBox.Focus();
                                htmlTextBox.SelectionStart = node.StreamPosition;
                                htmlTextBox.SelectionLength = node.OuterHtml.Length;
                                if(node.StreamPosition>=htmlTextBox.Text.Length)
                                    return;
                                var line = htmlTextBox.GetLineIndexFromCharacterIndex(node.StreamPosition); //返回指定字符串索引所在的行号
                                if (line > 0)
                                {
                                    htmlTextBox.ScrollToLine(line + 1); //滚动到视图中指定行索引
                                }
                            
                            });

                        
                        }
                    }


                    if (string.IsNullOrWhiteSpace(SelectName))
                    {
                        SelectName = string.Format(GlobalHelper.Get("key_651"),CrawlItems.Count);
                    }
                    (Commands2[1] as Command).Text = GlobalHelper.Get("key_652");
                }
                else
                {
                    if (htmlTextBox != null)

                    {
                        ControlExtended.UIInvoke(() =>
                        {
                            htmlTextBox.SelectionStart = 0;
                            htmlTextBox.SelectionLength = 0;
                        });
                    }
                    SelectXPath = "";
                    if (xpath_count > 1)
                    {
                        XLogSys.Print.Warn(GlobalHelper.Get("key_653"));
                        currentXPaths = HtmlDoc.SearchXPath(SelectText, () => IsAttribute).GetEnumerator();
                    }
                    else
                    {
                        var str = string.Format(GlobalHelper.Get("key_654"),SelectText, GlobalHelper.Get("key_639"));
                        if (isDynamicRemind == false)
                        {
                            XLogSys.Print.Info(str);
                        }
                        else
                        {
                            var str2 = String.Format(GlobalHelper.Get("not_find_key"),SelectText,GlobalHelper.Get("key_639"));

                            if (ConfigFile.Config.Get<bool>("AutoStartStopFiddler"))
                            { 
                                var res = MessageBox.Show(str2, GlobalHelper.Get("key_655"), MessageBoxButton.YesNoCancel);
                                switch (res)
                                {
                                    case MessageBoxResult.Yes:
                                        StartVisit();
                                        break;
                                    case MessageBoxResult.Cancel:
                                        isDynamicRemind = false;
                                        break;
                                }
                            }
                            else
                            {
                                  XLogSys.Print.Info(str2);                                
                            }
                        }
                     
                    }
                }
            }
            catch (Exception ex)
            {
                XLogSys.Print.Warn(string.Format(GlobalHelper.Get("key_656") , ex.Message));
            }
        }

        /// <summary>
        /// 是否提示自动嗅探对话框
        /// </summary>
        private bool isDynamicRemind = true;
        public void EditProperty()
        {
            var crawTargets = new List<XPathAnalyzer.CrawTarget>();
            crawTargets.Add(new XPathAnalyzer.CrawTarget(CrawlItems.Select(d => d.Clone()).ToList(), RootXPath,
                RootFormat) {RootNode = this.HtmlDoc.DocumentNode,WorkMode = IsMultiData});
            var luckModel = new FeelLuckyModel(crawTargets, HtmlDoc, IsMultiData);
            luckModel.CanChange = false;
            var view = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("key_657")) as UserControl;
            view.DataContext = luckModel;

            var name = GlobalHelper.Get("key_658");
            var window = new Window {Title = name};
            window.WindowState = WindowState.Maximized;
            window.Content = view;
            luckModel.SetView(view, window);

            window.Activate();
            window.ShowDialog();
            if (window.DialogResult == true)
            {
                CrawlItems.Clear();
                RootXPath = luckModel.CurrentTarget.RootXPath;
                CrawlItems.AddRange(luckModel.CurrentTarget.CrawItems);
            }
        }

        public void FeelLucky()
        {
            if (string.IsNullOrEmpty(this.URLHTML))
            {
                this.VisitUrlAsync();
            }
            isBusy = true;
            var crawTargets = new List<XPathAnalyzer.CrawTarget>();
            ICollection<CrawlItem> existItems = CrawlItems;
            if (IsMultiData == ScriptWorkMode.One)
                existItems = new List<CrawlItem> {new CrawlItem {Name = "temp", XPath = SelectXPath}};
            var task = TemporaryTask<XPathAnalyzer.CrawTarget>.AddTempTaskSimple(GlobalHelper.Get("key_659"),
                HtmlDoc.DocumentNode.SearchPropertiesSmart(IsMultiData, existItems, RootXPath, RootFormat, IsAttribute),
                crawTarget =>
                {
                    
                    crawTargets.Add(crawTarget);
                    //var datas =
                    //    HtmlDoc.DocumentNode.GetDataFromXPath(crawTarget.CrawItems, IsMultiData, crawTarget.RootXPath,
                    //        RootFormat).ToList();
                    //crawTarget.Datas = datas;
                }, d =>
                {
                    isBusy = false;
                    if (crawTargets.Count == 0)
                    {
                        XLogSys.Print.Warn(GlobalHelper.Get("key_660"));
                        return;
                    }

                    var luckModel = new FeelLuckyModel(crawTargets, HtmlDoc, IsMultiData);
                    var view = PluginProvider.GetObjectInstance<ICustomView>(GlobalHelper.Get("key_657")) as UserControl;
                    view.DataContext = luckModel;

                    var name = GlobalHelper.Get("feellucky");
                    var window = new Window {Title = name};
                    window.WindowState = WindowState.Maximized;
                    window.Content = view;
                    luckModel.SetView(view, window);
                    window.Activate();
                    window.ShowDialog();
                    if (window.DialogResult == true)

                    {
                        var crawTarget = luckModel.CurrentTarget;
                        if (string.IsNullOrEmpty(RootXPath))
                            RootFormat = SelectorFormat.XPath;
                        RootXPath = crawTarget.RootXPath;

                      
                        CrawlItems.AddRange(crawTarget.CrawItems.Where(r => r.IsEnabled&&CrawlItems.FirstOrDefault(d2=>d2.XPath==r.XPath)==null));
                    }
                });

            SysProcessManager.CurrentProcessTasks.Add(task);
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
            dict.Add("RootFormat", RootFormat);
            dict.Add("Remark", Remark);
            dict.Add("ShareCookie", ShareCookie.SelectItem);
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

        public void StartVisit()
        {
            if (IsRunning)
                return;
            if (string.IsNullOrWhiteSpace(SelectText) && ConfigFile.Config.Get<bool>("AutoStartStopFiddler")==true)
            {
                MessageBox.Show(GlobalHelper.Get("remind_10"));
                return;
            }
            ControlExtended.SafeInvoke(() =>
            {
                var url = URL;
                if (url.StartsWith("http") == false)
                    url = "http://" + url;

                CONFIG.IgnoreServerCertErrors = true;
                CONFIG.bMITM_HTTPS = true;
                FiddlerApplication.AfterSessionComplete += FiddlerApplicationAfterSessionComplete;
                var port = ConfigFile.Config.Get<int>("FiddlerPort");
                FiddlerApplication.Startup(port, true, true,true);
               
                System.Diagnostics.Process.Start(url);
                XLogSys.Print.Info(GlobalHelper.FormatArgs("fiddler_start", "localhost", port));
                OnPropertyChanged("IsRunning");
            }, LogType.Important, GlobalHelper.Get("key_661"));
        }

        private void FiddlerApplicationAfterSessionComplete(Session oSession)
        {
            if (oSession.oRequest.headers == null)
                return;

            var httpitem = new HttpItem {Parameters = oSession.oRequest.headers.ToString()};
            XLogSys.Print.Debug("visiting... "+ oSession.url);

            if ((oSession.BitFlags & SessionFlags.IsHTTPS) != 0)
            {
                httpitem.URL = "https://" + oSession.url;
            }
            else
            {
                httpitem.URL = "http://" + oSession.url;
            }
            if (oSession.RequestMethod.ToLower() == "post")
            {
                httpitem.Method = MethodType.POST;
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
            if (string.IsNullOrWhiteSpace(SelectText) == true)
            {
                return;
            }
            if (ConfigFile.Config.Get<bool>("AutoStartStopFiddler"))
                StopVisit();
            httpitem.DictCopyTo(Http);
            var post = "";
            if (Http.Method == MethodType.POST)
            {
                post = "POST content is:\n" + httpitem.Postdata + "\n";
            }
            var window = MainFrm as Window;
            ControlExtended.UIInvoke(() => { if (window != null) window.Topmost = true; });
            var info = GlobalHelper.FormatArgs("success_get",oSession.url,Http.Method,post);
            XLogSys.Print.Info(info);
            //IsSuperMode = false;
            ControlExtended.UIInvoke(() => { if (window != null) window.Topmost = false; });
            SniffSucceed?.Invoke(this, new EventArgs());
            URL = oSession.url;
        }

        public event EventHandler<EventArgs> SniffSucceed;

        public override bool Init()
        {
            var r = base.Init();
            hasInit = true;
            return r;
        }

        public override void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(dicts, scenario);
            URL = dicts.Set("URL", URL);
            RootXPath = dicts.Set("RootXPath", RootXPath);
            Remark = dicts.Set("Remark", Remark);
            RootFormat = dicts.Set("RootFormat", RootFormat);
            ShareCookie.SelectItem = dicts.Set("ShareCookie", ShareCookie.SelectItem);
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
            var rootPath = RootXPath;
         
            if (!string.IsNullOrEmpty(rootPath))
            {
                //TODO: 当XPath路径错误时，需要捕获异常
                HtmlNode root = null;
                try
                {
                    root = HtmlDoc.DocumentNode.SelectSingleNodePlus(rootPath, RootFormat);
                }
                catch (Exception )
                {
                    XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_662"),RootXPath,RootFormat));
                }
                if (!(root != null).SafeCheck(string.Format(GlobalHelper.Get("key_663"), RootFormat,RootXPath)))
                    return;
                root = HtmlDoc.DocumentNode.SelectSingleNodePlus(rootPath, RootFormat)?.ParentNode;

                HtmlNode node = null;
                if (
                    !ControlExtended.SafeInvoke(() => HtmlDoc.DocumentNode.SelectSingleNodePlus(path, SearchFormat),
                        ref node,
                        LogType.Info, GlobalHelper.Get("key_664"), true))

                {
                    return;
                }
                if (!(node != null).SafeCheck(GlobalHelper.Get("key_665")))
                    return;

                if (!node.IsAncestor(root) && isAlert)
                {
                    if (
                        MessageBox.Show(GlobalHelper.Get("key_666"), GlobalHelper.Get("key_99"), MessageBoxButton.YesNo) ==
                        MessageBoxResult.No)
                    {
                        return;
                    }
                }
                string attr = "";
                string attrValue = "";
                XPathAnalyzer.GetAttribute(path, out attr, out attrValue);
                if (SearchFormat == SelectorFormat.XPath)
                {
                    path = XPath.TakeOffPlus(node.XPath, root.XPath);
                    if (attr != "")
                        path += "/@" + attr + "[1]";
                  
                }

            }
            if (CrawlItems.FirstOrDefault(d => d.Name == SelectName) == null ||
                MessageBox.Show(GlobalHelper.Get("add_column_sure"), GlobalHelper.Get("key_99"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var item = new CrawlItem {XPath = path, Name = SelectName, SampleData1 = SelectText};
                item.Format = SearchFormat;
                CrawlItems.Add(item);

                SelectXPath = "";
                SelectName = "";

                XLogSys.Print.Info(GlobalHelper.Get("key_668"));
            }
        }

        public IEnumerable<FreeDocument> CrawlData(HtmlNode doc)
        {
            if (CrawlItems.Count == 0)
            {
                var freedoc = new FreeDocument {{"Content", doc.OuterHtml}};

                return new List<FreeDocument> {freedoc};
            }

            return doc.GetDataFromXPath(CrawlItems, IsMultiData, RootXPath, RootFormat);
        }

        public string GetHtml(string url, out HttpStatusCode code,
            string post = null)
        {
            string result = "";
            HttpHelper.HttpResponse response;
            code = HttpStatusCode.NotFound;
            if (Regex.IsMatch(url, @"^[A-Z]:\\")) //本地文件
            {
                if (File.Exists(url))
                {
                    result = File.ReadAllText(url, AttributeHelper.GetEncoding(this.Http.Encoding));
                    code = HttpStatusCode.Accepted;
                }
              
              
            }
            else
            {


                var mc = extract.Matches(url);
                if (SysProcessManager == null)
                {
                    code = HttpStatusCode.NoContent;
                    return "";
                }
                var crawler = this.SysProcessManager.GetTask<SmartCrawler>( ShareCookie.SelectItem);
                if (crawler != null)
                {
                    Http.ProxyIP = crawler.Http.ProxyIP;
                    if (Http.Parameters != crawler.Http.Parameters)
                    {
                        var cookie = crawler.Http.GetHeaderParameter().Get<string>("Cookie");
                        if (string.IsNullOrWhiteSpace(cookie) == false)
                        {
                            Http.SetValue("Cookie", cookie);
                        }
                    }
                }
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
                 response = helper.GetHtml(Http,  url, post).Result;
                 result = response.Html;
                code = response.Code;

            }
            result = JavaScriptAnalyzer.Decode(result);
            if (IsSuperMode)
            {
                result = JavaScriptAnalyzer.Parse2XML(result);
            }

            return result;
        }

        public IEnumerable<FreeDocument> CrawlData(string url, out HtmlDocument doc, out HttpStatusCode code,
            string post = null)
        {
            ConfigFile.GetConfig<DataMiningConfig>().RequestCount++;
            var content = GetHtml(url, out code, post);
           
                var datas = CrawlHtmlData(content, out doc);
              
                return datas;
          


        }

        public IEnumerable<FreeDocument> CrawlHtmlData(string html, out HtmlDocument doc
            )
        {
            doc = new HtmlDocument();

            doc.LoadHtml(html);


            return CrawlData(doc.DocumentNode);
        }

        private async void VisitUrlAsync()
        {
            if (!enableRefresh)
                return;
            if (hasInit == false)
                return;

            URLHTML = await MainFrm.RunBusyWork(() =>
            {
                HttpStatusCode code;
                ConfigFile.GetConfig<DataMiningConfig>().RequestCount++;
                return GetHtml(URL, out code);
            },title:GlobalHelper.Get("long_visit_web"));
            if (URLHTML.Contains(GlobalHelper.Get("key_671")) &&
                MessageBox.Show(GlobalHelper.Get("key_672") + URLHTML + GlobalHelper.Get("key_673"), GlobalHelper.Get("key_99"),
                    MessageBoxButton.OK) == MessageBoxResult.OK)

            {
                return;
            }


            ControlExtended.SafeInvoke(() =>
            {
                HtmlDoc.LoadHtml(URLHTML);
                if (MainDescription.IsUIForm)
                {
                    var dock = MainFrm as IDockableManager ?? ControlExtended.DockableManager;
                    var control = dock?.ViewDictionary.FirstOrDefault(d => d.Model == this);
                    if (control != null)
                    {
                        dynamic invoke = control.View;
                        if (IsSuperMode == false)
                        {
                            invoke.UpdateHtml(URLHTML);
                            OnPropertyChanged("HtmlDoc");
                        }
                        else
                        {
                            invoke.UpdateHtml(GlobalHelper.Get("key_674"));
                        }
                    }
                }
            },
                name: GlobalHelper.Get("key_675"));


            if (string.IsNullOrWhiteSpace(selectText) == false)
            {
                currentXPaths = HtmlDoc.SearchXPath(SelectText, () => IsAttribute).GetEnumerator();
                GetXPathAsync();
            }
            OnPropertyChanged("URLHTML");
        }
    }
}