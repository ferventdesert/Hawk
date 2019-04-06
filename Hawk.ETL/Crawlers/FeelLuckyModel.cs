using System.Collections.Generic;
using Hawk.Core.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.MVVM;
using HtmlAgilityPack;

namespace Hawk.ETL.Crawlers
{
    public class FeelLuckyModel : PropertyChangeNotifier
    {
        private readonly HtmlDocument HtmlDoc;
        private Window parent;
        private int position;
        private SortMethod sortMethod;
        private dynamic View;

        public FeelLuckyModel(List<XPathAnalyzer.CrawTarget> crawTargets, HtmlDocument htmlDoc, ScriptWorkMode scriptWorkMode
           )
        {
            CrawTargets = crawTargets;
            SortMethod = scriptWorkMode == ScriptWorkMode.List ? SortMethod.SortByArea : SortMethod.SortByColumn; ;
            var targets = CrawTargets.OrderByDescending(sortLogic)
                ;
            this.ScriptWorkMode = scriptWorkMode;
            CanChange = true;
            CrawTargetView = new ListCollectionView(targets.ToList());
            Position = 0;
            HtmlDoc = htmlDoc;
         
        }

        public ScriptWorkMode ScriptWorkMode { get; set; }

        public ListCollectionView CrawTargetView { get; set; }
        private List<XPathAnalyzer.CrawTarget> CrawTargets { get; }
        public XPathAnalyzer.CrawTarget CurrentTarget => CrawTargetView.GetItemAt(Position) as XPathAnalyzer.CrawTarget;
        public bool CanChange { get; set; }

        public int Position
        {
            get { return position; }
            set
            {
                if (position != value)
                {
                    position = value;
                    OnPropertyChanged("Position");
                    OnPropertyChanged("CurrentTarget");
                    View.SetContent(CurrentTarget.Datas);
                }
            }
        }

        public SortMethod SortMethod
        {
            get { return sortMethod; }
            set
            {
                if (sortMethod != value)
                {
                    sortMethod = value;
                    this.Position = 0;
                    CrawTargetView = new ListCollectionView(CrawTargets.OrderByDescending(sortLogic).ToList());
                    OnPropertyChanged("SortMethod");
                    OnPropertyChanged("CrawTargetView");
                    OnPropertyChanged("Position");
                    OnPropertyChanged("CurrentTarget");
                    View?.SetContent(CurrentTarget.Datas);
                }
            }
        }

        public int Total => CrawTargets.Count - 1;
        private ObservableCollection<CrawlItem> CrawlItems => CurrentTarget.CrawItems;

        [Browsable(false)]
        public ReadOnlyCollection<ICommand> CrawlItemCommands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_84"),
                            d =>
                            {
                                CrawlItems.Add(new CrawlItem {Name = GlobalHelper.Get("propery")+"_" + CrawlItems.Count});
                            }, null, "add"),
                        new Command(GlobalHelper.Get("key_166"),
                            d => CrawlItems.Execute(d2 => d2.IsSelected = true), null, "check"),
                        new Command(GlobalHelper.Get("key_167"),
                            d => CrawlItems.Execute(d2 => d2.IsSelected = !d2.IsSelected), null, "redo"),
                        new Command(GlobalHelper.Get("key_168"),
                            d =>
                            {
                                var items = CrawlItems.Where(d2 => d2.IsSelected).ToList();
                                foreach (var item in items)
                                {
                                    var newItem = new CrawlItem();
                                    item.DictCopyTo(newItem);
                                    CrawlItems.Add(newItem);
                                }
                            }, null, "page_add"),
                        new Command(GlobalHelper.Get("key_169"),
                            d =>
                            {
                                CrawlItems.RemoveElementsNoReturn(d2 => d2.IsSelected);
                            }, null, "delete")
                    });
            }
        }

        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_142"), obj => { Refresh(); }, icon: "refresh"),
                        new Command(GlobalHelper.Get("key_170"), obj =>
                        {
                            if (Position > 0)
                                Position--;
                        }, obj => Position > 0, "arrow_left"),
                        new Command(GlobalHelper.Get("next"), obj => { Position++; }, obj => Position < Total, "arrow_right"),
                        new Command(GlobalHelper.Get("key_172"), obj =>
                        {
                            parent.DialogResult = true;
                            parent.Close();
                        }, icon: "check"),
                        new Command(GlobalHelper.Get("key_173"), obj =>
                        {
                            parent.DialogResult = false;
                            parent.Close();
                        }, icon: "close")
                    }
                    );
            }
        }

        private double sortLogic(XPathAnalyzer.CrawTarget d)
        {
            var item = d.Score;
            if (sortMethod == SortMethod.SortByRow)
            {
                return d.NodeCount;
            }
            if (sortMethod == SortMethod.SortByColumn)
            {
                return d.ColumnCount;
            }
            if (sortMethod == SortMethod.SortByArea)
            {
                return d.ColumnCount*d.NodeCount;
            }
            return item;
        }

        public void SetView(object view, Window window)
        {
            View = view;
            View.SetContent(CurrentTarget.Datas);
            parent = window;
            Refresh();
        }

        private void Refresh()
        {
           
            if (View != null)
                View.SetContent(CurrentTarget.Datas);
        }
    }
}