using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Xceed.Wpf.DataGrid;

namespace Hawk.ETL.Controls.DataViewers
{
    /// <summary>
    /// DataGridViewUI.xaml 的交互逻辑
    /// </summary>
    public partial class DataGridViewUI : UserControl
    {
        public DataGridViewUI()
        {
            InitializeComponent();
        }
    }
    [XFrmWork("key_230", "DataGridViewer_desc")]
    public class DataGridViewer : PropertyChangeNotifier, IDataViewer
    {
        public object SetCurrentView(IEnumerable<IFreeDocument> datas)
        {
            if (!datas.Any())
            {
                return null;
                //    throw new Exception("不存在任何数据");
                ;
            }
            var listview = new DataGridViewUI();

            foreach (var data in datas.GetKeys())
            {
                listview.DataGridControl.Columns.Add(new Column { Title = data, FieldName = $"[{data}]"});
            }
            listview.DataContext = datas;
            return listview;
        }

        public bool IsEditable => true;
    }
}
