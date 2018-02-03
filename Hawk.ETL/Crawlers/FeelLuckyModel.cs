using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public FeelLuckyModel(List<XPathAnalyzer.CrawTarget> crawTargets, HtmlDocument htmlDoc,
            SortMethod sortMethod = SortMethod.按面积排序)
        {
            CrawTargets = crawTargets;
            SortMethod = sortMethod;
            var targets = CrawTargets.OrderByDescending(sortLogic)
            ;
           
            CrawTargetView = new ListCollectionView(targets.ToList());
            Position = 0;
            HtmlDoc = htmlDoc;
        }

        double sortLogic(XPathAnalyzer.CrawTarget d)
        {
            var item = d.Score;
            if (sortMethod == SortMethod.按行数排序)
            {
                return d.NodeCount;
            }
            if (sortMethod == SortMethod.按列数排序)
            {
                return d.ColumnCount;
            }
            if (sortMethod == SortMethod.按面积排序)
            {
                return d.ColumnCount * d.NodeCount;
            }
            return  item;
        }

        public ListCollectionView CrawTargetView { get; set; }
        private List<XPathAnalyzer.CrawTarget> CrawTargets { get; }
        public XPathAnalyzer.CrawTarget CurrentTarget => CrawTargetView.GetItemAt(Position) as XPathAnalyzer.CrawTarget;

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
                    CrawTargetView = new ListCollectionView( CrawTargets.OrderByDescending(sortLogic).ToList());
                    OnPropertyChanged("SortMethod");
                    OnPropertyChanged("CrawTargetView");
                    OnPropertyChanged("Position");
                    OnPropertyChanged("CurrentTarget");
                    View?.SetContent(CurrentTarget.Datas);
                }
            }
        }

        public int Total => CrawTargets.Count - 1;

        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("刷新", obj => { Refresh(); }, icon: "refresh"),
                        new Command("上一个", obj =>
                        {
                            if (Position > 0)
                                Position--;
                        }, obj => Position > 0, "arrow_left"),
                        new Command("下一个", obj => { Position++; }, obj => Position < Total, "arrow_right"),
                        new Command("确认结果", obj =>
                        {
                            parent.DialogResult = true;
                            parent.Close();
                        },icon:"check"),
                        new Command("退出", obj =>
                        {
                            parent.DialogResult = false;
                            parent.Close();
                        },icon:"close")
                    }
                    );
            }
        }

        public void SetView(object view, Window window)
        {
            View = view;
            View.SetContent(CurrentTarget.Datas);
            parent = window;
        }

        private void Refresh()
        {
            CurrentTarget.Datas = HtmlDoc.DocumentNode.GetDataFromXPath(CurrentTarget.CrawItems.Where(d => d.IsEnabled).ToList(),
                ScriptWorkMode.List,
                CurrentTarget.RootXPath,CurrentTarget.RootFormat);
            if (View != null)
                View.SetContent(CurrentTarget.Datas);
        }
    }
}