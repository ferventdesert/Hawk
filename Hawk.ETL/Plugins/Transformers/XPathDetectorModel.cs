using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private int _childCount;
        private string _xPath;
        private Window view;
        private TextBox htmlTextBox;
        private string selectText = "";
        private string urlHTML;
        private CrawlItem _selectedItem;

        public XPathDetectorModel(string html, ScriptWorkMode workmode,Window theView,TextBox textbox)
        {
            HtmlDoc = new HtmlDocument();

            ControlExtended.SafeInvoke(() => HtmlDoc.LoadHtml(html));
            URLHTML = html;
            view = theView;
            htmlTextBox = textbox; 
            if (workmode == ScriptWorkMode.List)
                ChildCount = 5;
            else
            {
                ChildCount = 1;
            }
            CrawlItems=new ObservableCollection<CrawlItem>();
            ChildItems=new ObservableCollection<CrawlItem>();
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

        public string XPath
        {
            get { return _xPath; }
            set
            {
                if (_xPath != value)
                {
                    _xPath = value;
                    OnPropertyChanged("XPath");
                }
            }
        }

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

        public CrawlItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;

                    XPath = value.XPath;

                    if (htmlTextBox != null)
                    {
                        ControlExtended.UIInvoke(() =>
                        {
                            var node = HtmlDoc.DocumentNode.SelectSingleNodePlus(XPath, SelectorFormat.XPath);
                            htmlTextBox.Focus();
                            htmlTextBox.SelectionStart = node.StreamPosition;
                            htmlTextBox.SelectionLength = node.OuterHtml.Length;
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
                        new Command(GlobalHelper.Get("key_652"),  obj =>Search() , icon: "refresh"),
                        new Command(GlobalHelper.Get("key_652"),  obj =>SearchChild() , icon: "refresh"),
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
            if (string.IsNullOrWhiteSpace(XPath) == false)
            {
                List<HtmlNode> nodes = null;
                ControlExtended.SafeInvoke(()=>nodes= HtmlDoc.DocumentNode.SelectNodesPlus(XPath, SelectorFormat.XPath).ToList());
                ChildItems.Clear();
                if (nodes == null)
                {
                    XLogSys.Print.Info(GlobalHelper.Get("key_665"));
                    return;
                }
                nodes.Execute(d => ChildItems.Add(new CrawlItem()
                {
                    XPath = d.XPath,
                    SampleData1 =d.InnerHtml
                }));
            }
        }

        private void Search()
        {
            if (string.IsNullOrWhiteSpace(selectText) == false)
            {
                var xpaths = HtmlDoc.SearchXPath(SelectText, () => true).ToList();
                CrawlItems.Clear();
                xpaths.Execute(d=>CrawlItems.Add(new CrawlItem()
                {
                    XPath = d,
                    SampleData1 = HtmlDoc.DocumentNode.SelectSingleNodePlus(d,SelectorFormat.XPath).InnerText
                }));
            }
        }
    }
}