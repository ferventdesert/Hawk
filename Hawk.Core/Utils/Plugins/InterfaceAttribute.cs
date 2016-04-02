using System;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 该类定义了插件系统的接口契约记录
    /// </summary>
     [Serializable]
    public class InterfaceAttribute : System.Attribute     
    {
        public InterfaceAttribute()
        {
        }


        public string Name { get; set; }
        /// <summary>
        /// 相关信息
        /// </summary>
        public string Description { get; set; }

        public InterfaceAttribute(string thisDetailInfo=null)
        // 定位参数
        {
           
            this.Description = thisDetailInfo;

        }

      
    }
}
