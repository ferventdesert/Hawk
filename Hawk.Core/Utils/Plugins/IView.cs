namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 实现界面效果必须满足的接口
    /// </summary>
    public interface IView 
    {
        /// <summary>
        /// 要显示的内容
        /// </summary>
        object UserControl { get;   }
        /// <summary>
        /// 要显示的位置和方式，这随着系统的不同定义也有所不同
        /// </summary>
        FrmState FrmState { get; }


    }

    public enum GenerateMode
    {
        串行模式,
        并行模式
    }


    /// <summary>
    /// 显示位置
    /// </summary>
    public enum FrmState
    {
        Buttom,
        Mini,
        Middle,
        Large,
        Float,
        Mini2,
        /// <summary>
        /// 用于项目内部自身处理需求
        /// </summary>
        Custom,
        /// <summary>
        /// 隐藏
        /// </summary>
        Hide,
        /// <summary>
        /// 不存在
        /// </summary>
        Null
    }
}