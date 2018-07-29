using System;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 自定义的Attribute,可在框架中提供程序集名称等信息
    /// </summary>
    [Serializable]
    public class XFrmWorkAttribute : Attribute 
    {
        #region Constants and Fields

        /// <summary>
        /// 所标记的类型
        /// </summary>
        [NonSerialized] public Type MyType;

        public XFrmWorkAttribute Self => this;

        #endregion

        #region Constructors and Destructors

        public XFrmWorkAttribute(string name,  string description = "", string url = "", string groupName = "默认分组",int order=0)
        {
            this.name = name; 
            this.description = description; 
            this.GroupName = groupName;
            this.LogoURL = url;
            this.Order = order;
        }

        private string name;
        private string description;

        public XFrmWorkAttribute()
        {
        }
        public override string ToString()
        {
            return Name  ;
        }

        /// <summary>
        /// 加载顺序
        /// </summary>
        public int Order { get; set; }

        #endregion

        #region Properties
        /// <summary>
        /// 描述
        /// </summary>
        public string Description {
            get { return GlobalHelper.Get(description); }
        }

        /// <summary>
        /// 管理分级机制，其具体定义和分级方法由插件管理器决定
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 图片URL
        /// </summary>
        public string LogoURL { get; set; }

        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name {
            get
            {
                return GlobalHelper.Get(name);
            }
        }

        #endregion
    }


    /// <summary>
    /// 对某项插件使用忽略标志，不会认为是PluginName的一个插件
    /// </summary>
    public class XFrmWorkIgnore : Attribute
    {
      
     


    }
}