using Hawk.Core.Connectors;

namespace Hawk.ETL.Interfaces
{
    public interface IColumnDataFilter :  IColumnProcess
    {
        bool FilteData(IFreeDocument data);
        bool IsDebugFilter { get; set; }

    }
}