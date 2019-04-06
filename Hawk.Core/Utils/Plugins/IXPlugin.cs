namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// XFrmwork基本插件接口
    /// </summary>
    [Interface("IXPlugin" )]
    public interface IXPlugin : IProcess
    {
        #region Properties

        /// <summary>
        /// 主框架引用
        /// </summary>
        IMainFrm MainFrmUI { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// 保存配置文件
        /// </summary>
        void SaveConfigFile();

        #endregion
    }
}