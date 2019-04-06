using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.ETL.Crawlers;
using HtmlAgilityPack;

namespace Hawk.ETL.Plugins.Transformers
{
    public class XPathDetectorModel : PropertyChangeNotifier
    {
        private readonly TextBox htmlTextBox;
        private readonly Window view;
        private int _childCount;
        private CrawlItem _selectedItem;
        private HtmlResult _selectedResult;
        private string _xPath;
        private string selectText = "";
        private string urlHTML;

        public XPathDetectorModel(IEnumerable<HtmlResult> htmlResults, ScriptWorkMode workmode, Window theView,
            TextBox textbox)
        {
            HtmlDoc = new HtmlDocument();
            var xpathHelper = new Dictionary<string, string>
            {
                {"all_image", "//img[@src]"},
                {"all_item_with_id", @"//*[@id=""YOUR_ID""]"},
                {"all_item_with_class", @"//*[@class=""YOUR_CLASS""]"}
            };

            HtmlResults = htmlResults.ToList();
            view = theView;
            htmlTextBox = textbox;
            XPath = new TextEditSelector();
            XPath.SetSource(xpathHelper.Select(d => d.Value));
            if (workmode == ScriptWorkMode.List)
                ChildCount = 5;
            else
            {
                ChildCount = 1;
            }
            CrawlItems = new ObservableCollection<CrawlItem>();
            ChildItems = new ObservableCollection<CrawlItem>();
            SelectedResult = HtmlResults.FirstOrDefault();
        }

        public List<HtmlResult> HtmlResults { get; set; }

        public HtmlResult SelectedResult
        {
            get { return _selectedResult; }
            set
            {
                if (_selectedResult != value)
                {
                    _selectedResult = value;

                    OnPropertyChanged("SelectedResult");
                    OnPropertyChanged("URLHtml");
                    ControlExtended.SafeInvoke(() => HtmlDoc.LoadHtml(SelectedResult.HTML));
                }
            }
        }

        public int ChildCount
        {
            get { return _childCount; }
            set
            {
                if (_childCount != value)
                {
                    _childCount = value;
                    OnPropertyChanged("ChildCount");
                }
            }
        }

        public ObservableCollection<CrawlItem> CrawlItems { get; set; }
        public ObservableCollection<CrawlItem> ChildItems { get; set; }
        public TextEditSelector XPath { get; set; }

        [Browsable(false)]
        public string SelectText
        {
            get { return selectText; }
            set
            {
                if (selectText == value) return;
                selectText = value;

                OnPropertyChanged("SelectText");
            }
        }

        [Browsable(false)]
        public string URLHTML => SelectedResult?.HTML;

        public CrawlItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;

                    XPath._SelectItem = value.XPath;

                    if (htmlTextBox != null)
                    {
                        ControlExtended.UIInvoke(() =>
                        {
                            var node = HtmlDoc.DocumentNode.SelectSingleNodePlus(XPath.SelectItem, SelectorFormat.XPath);
                            htmlTextBox.Focus();
                            htmlTextBox.SelectionStart = node.StreamPosition;
                            htmlTextBox.SelectionLength = node.OuterHtml.Length;
                            if (node.StreamPosition > htmlTextBox.Text.Length)
                                return;
                            var line = htmlTextBox.GetLineIndexFromCharacterIndex(node.StreamPosition); //返回指定字符串索引所在的行号
                            if (line > 0)
                            {
                                htmlTextBox.ScrollToLine(line + 1); //滚动到视图中指定行索引
                            }
                        });
                    }
                }
            }
        }

        public HtmlDocument HtmlDoc { get; set; }

        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_652"), obj => Search(), icon: "refresh"),
                        new Command(GlobalHelper.Get("key_624"), obj => SearchChild(), icon: "refresh"),
                        new Command(GlobalHelper.Get("key_172"), obj =>
                        {
                            view.DialogResult = true;

                            view.Close();
                        }, icon: "check"),
                        new Command(GlobalHelper.Get("key_173"), obj =>
                        {
                            view.DialogResult = false;
                            view.Close();
                        }, icon: "close")
                    }
                    );
            }
        }

        private void SearchChild()
        {
            if (string.IsNullOrWhiteSpace(XPath.SelectItem) == false)
            {
                List<HtmlNode> nodes = null;
                ControlExtended.SafeInvoke(
                    () => nodes = HtmlDoc.DocumentNode.SelectNodesPlus(XPath.SelectItem, SelectorFormat.XPath).ToList());
                ChildItems.Clear();
                if (nodes == null)
                {
                    XLogSys.Print.Info(GlobalHelper.Get("key_665"));
                    return;
                }
                nodes.Execute(d => ChildItems.Add(new CrawlItem
                {
                    XPath = d.XPath,
                    SampleData1 = d.InnerHtml
                }));
            }
        }

        private void Search()
        {
            if (string.IsNullOrWhiteSpace(selectText) == false)
            {
                var xpaths = HtmlDoc.SearchXPath(SelectText, () => true).ToList();
                CrawlItems.Clear();
                xpaths.Execute(d => CrawlItems.Add(new CrawlItem
                {
                    XPath = d,
                    SampleData1 = HtmlDoc.DocumentNode.SelectSingleNodePlus(d, SelectorFormat.XPath).InnerText
                }));
            }
        }

        public class HtmlResult
        {
            public string Url { get; set; }
            public string HTML { get; set; }
        }
    }
}