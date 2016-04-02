namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 处理方法的公共基接口
    /// </summary>
    public interface IProcess
    {

      
        string Name { get; set; }

        /// <summary>
        /// 公开类型
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// 数据处理函数
        /// </summary>
        /// <returns></returns>
        bool Process();
        /// <summary>
        /// 数据初始化
        /// </summary>
        /// <returns></returns>
        bool Init();

        /// <summary>
        /// 是否开启
        /// </summary>
        bool IsOpen { get;  }
        /// <summary>
        /// 关闭所需的处理函数
        /// </summary>
        /// <returns></returns>
        bool Close();


    }
}
