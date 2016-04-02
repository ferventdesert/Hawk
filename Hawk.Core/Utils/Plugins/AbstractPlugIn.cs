using System;
using Hawk.Core.Utils.MVVM;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 抽象插件基类，默认已经实现了IXPlugin接口
    /// <seealso cref="IXPlugin"/>
    /// </summary>
    public abstract class AbstractPlugIn : PropertyChangeNotifier, IXPlugin
    {
        #region Events

        protected AbstractPlugIn()
        {
            this.Name = TypeName;
        }

       

        #endregion

        #region Properties

        public bool IsOpen { get; set; }

        /// <summary>
        /// 主界面窗口引用
        /// </summary>
        public IMainFrm MainFrmUI { get; set; }

        public string Name { get; set; }

        public string TypeName => AttributeHelper.GetCustomAttribute(this.GetType()).Name;

        #endregion

        #region Implemented Interfaces

        #region IProcess

        public virtual bool Close()
        {
            return true;
        }

        public virtual bool Process()
        {
            return true;
        }

        public virtual bool Init()
        {
            return true;
        }

        #endregion

        #region IXPlugin

        public virtual void SaveConfigFile()
        {
        }

        #endregion

        #endregion
    }
}