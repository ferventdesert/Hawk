using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Hawk.Core.Utils.MVVM;

namespace Hawk.ETL.Crawlers
{
    public enum SortMethod
    {
        ByColumnCount,
        ByItemCount,
        ByScore
    }

    public class FeelLuckyModel : PropertyChangeNotifier
    {
        private Window parent;
        private int position;
        private string sortMethod;
        private dynamic View;

        public FeelLuckyModel(List<XPathAnalyzer.CrawTarget> crawTargets)
        {
            CrawTargets = crawTargets;
            CrawTargetView = new ListCollectionView(CrawTargets);
            Position = 0;

            SortMethods = new List<string> {"ByColumnCount", "ByItemCount", "ByScore"};
            SortMethod = "ByScore";
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
                        if (value == "ByItemCount")
                        {
                            return d.NodeCount;
                        }
                        if (value == "ByColumnCount")
                        {
                            return d.ColumnCount;
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

        public int Total => CrawTargets.Count;

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
                        new Command("下一个", obj => { Position++; }, obj => Position < Total - 1, "arrow_right"),
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
        }
    }
}