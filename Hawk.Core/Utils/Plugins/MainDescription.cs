namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 应用程序主入口描述器
    /// </summary>
    public class MainDescription
    {
        /// <summary>
        /// 指示当前主程序是WPF模型
        /// </summary>
        public static bool IsUIForm = true;

        /// <summary>
        /// 主插件容器
        /// </summary>
        public static IMainFrm MainFrm { get; set; }

    }
}
