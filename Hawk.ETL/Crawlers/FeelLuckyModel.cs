using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Hawk.Core.Utils.MVVM;
using HtmlAgilityPack;

namespace Hawk.ETL.Crawlers
{
    public enum SortMethod
    {
        按列数排序,
        按行数排序,
        按分数排序
    }

    public class FeelLuckyModel : PropertyChangeNotifier
    {
        private readonly HtmlDocument HtmlDoc;
        private Window parent;
        private int position;
        private string sortMethod;
        private dynamic View;

        public FeelLuckyModel(List<XPathAnalyzer.CrawTarget> crawTargets, HtmlDocument htmlDoc)
        {
            CrawTargets = crawTargets;
            CrawTargetView = new ListCollectionView(CrawTargets);
            Position = 0;
            HtmlDoc = htmlDoc;
            SortMethods = new List<string> {"按列数排序", "按行数排序", "按分数排序", "按面积排序"};
            SortMethod = "按面积排序";
        }

        public List<string> SortMethods { get; set; }
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

        public string SortMethod
        {
            get { return sortMethod; }
            set
            {
                if (sortMethod != value)
                {
                    sortMethod = value;
                    CrawTargetView = new ListCollectionView(CrawTargets.OrderByDescending(d =>
                    {
                        var item = d.Score;
                        if (value == "按行数排序")
                        {
                            return d.NodeCount;
                        }
                        if (value == "按列数排序")
                        {
                            return d.ColumnCount;
                        }
                        if (value == "按面积排序")
                        {
                            return d.ColumnCount*d.NodeCount;
                        }
                        return item;
                    }).ToList());
                    OnPropertyChanged("SortMethod");
                    OnPropertyChanged("CrawTargetView");
                    OnPropertyChanged("Position");
                    OnPropertyChanged("CurrentTarget");
                    View?.SetContent(CurrentTarget.Datas);
                }
            }
        }

        public int Total => CrawTargets.Count-1;

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
                        new Command("下一个", obj => { Position++; }, obj => Position < Total , "arrow_right"),
                        new Command("确认结果", obj =>
                        {
                            parent.DialogResult = true;
                            parent.Close();
                        }),
                        new Command("退出", obj =>
                        {
                            parent.DialogResult = false;
                            parent.Close();
                        })
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
            CurrentTarget.Datas = HtmlDoc.GetDataFromXPath(CurrentTarget.CrawItems.Where(d=>d.IsEnabled).ToList(), ListType.List,
                CurrentTarget.RootXPath);
            if (View != null)
                View.SetContent(CurrentTarget.Datas);
        }
    }
}