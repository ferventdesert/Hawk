using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Xceed.Wpf.DataGrid;

namespace Hawk.ETL.Controls
{


    /// <summary>
    ///     FeelLuckPanel.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("手气不错面板")]
    public partial class FeelLuckPanel : UserControl, ICustomView
    {
        public FeelLuckPanel()
        {
            InitializeComponent();
        }

        public FrmState FrmState { get; }

        public void SetContent(List<FreeDocument> datas)

        {
            DataGridControl.Columns.Clear();
            if (datas == null || datas.Count == 0)
                return;
            foreach (var data in datas.GetKeys())
            {
                DataGridControl.Columns.Add(new Column {Title = data, FieldName = $"[{data}]"});
            }
            DataGridControl.DataContext = datas;
        }
    }
}