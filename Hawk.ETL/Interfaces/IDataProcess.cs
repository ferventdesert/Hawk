using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Interfaces
{
    /// <summary>
    ///  数据处理方法的公共基接口
    /// </summary>
    [Interface("IDataProcess" )]
    public interface IDataProcess : IProcess
    {
        #region Properties

         
        /// <summary>
        /// 系统提供的数据管理 
        /// </summary>
        IDataManager SysDataManager { get; set; }

        /// <summary>
        /// 系统的处理管理器实例
        /// </summary>
        IProcessManager SysProcessManager { get; set; }


        #endregion
    }
}