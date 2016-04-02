using Hawk.Core.Utils.MVVM;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 在系统界面中显示菜单的接口
    /// </summary>
    public interface IMainFrmMenu
    {
        IAction BindingCommands { get; }
    }
}