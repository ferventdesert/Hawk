using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Hawk.Core.Utils.MVVM;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 主框架接口
    /// </summary>
    public interface IMainFrm
    {
        #region Properties

        /// <summary>
        /// 用于在主界面上显示菜单的绑定命令
        /// </summary>
        ObservableCollection<IAction> CommandCollection { get; set; }

        /// <summary>
        /// 插件文件夹的搜索位置
        /// </summary>
        string MainPluginLocation { get; }

        /// <summary>
        /// 当前系统存储的插件字典
        /// <remarks>您可以通过提供该插件的名称，以获取该插件，需要进行数据转换</remarks>
        /// <example>获取数据服务器IServerSystem可以用如下方法进行
        /// var server= myPluginDictionary["数据库管理器"] as server;
        /// 因此，插件名称应该稳定，否则，字典将无法工作
        /// </example>
        /// </summary>
        Dictionary<string, IXPlugin> PluginDictionary { get; set; }

        event EventHandler<ProgramEventArgs> ProgramEvent;

        void InvokeProgramEvent(ProgramEventArgs e);

        #endregion
    }

    public enum ProgramEventType
    {
        /// <summary>
        /// 用户注销
        /// </summary>
        UserLogoff,
        /// <summary>
        /// 系统关闭
        /// </summary>
        SystemClose,
        /// <summary>
        /// 强行关闭
        /// </summary>
        ForceClose,
    }
    public class ProgramEventArgs : EventArgs
    {
        public ProgramEventArgs(ProgramEventType type)
        {
            this.Type = type;
        }

        public ProgramEventType Type { get; set; }
    }
}