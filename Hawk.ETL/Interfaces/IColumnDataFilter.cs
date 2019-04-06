using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;

namespace Hawk.ETL.Interfaces
{

    public enum FilterWorkMode
    {
        [LocalizedDescription("filter_mode_by_item")]
        ByItem,
        [LocalizedDescription("filter_mode_pass_when_success")]
        PassWhenSuccess,
        [LocalizedDescription("filter_mode_stop_when_fail")]
        StopWhenFail
    }
    public interface IColumnDataFilter :  IColumnProcess
    {
        bool FilteData(IFreeDocument data);
        bool IsDebugFilter { get; set; }
        FilterWorkMode FilterWorkMode { get; set; }


    }
}