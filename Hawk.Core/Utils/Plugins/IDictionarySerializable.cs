using System.Collections.Generic;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 应用场景，通常用于数据导入导出的显示问题
    /// </summary>
    public  enum  Scenario
    {
        /// <summary>
        /// 界面
        /// </summary>
        UI,
        /// <summary>
        /// 数据库
        /// </summary>
        Database,
        /// <summary>
        /// 数据报告
        /// </summary>
        Report,
        /// <summary>
        /// 用于数据绑定
        /// </summary>
        Binding,
        /// <summary>
        /// 搜索
        /// </summary>
        Search,
        /// <summary>
        /// 控制
        /// </summary>
        Control,
        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    /// <summary>
    /// 进行字典序列化的接口，方便实现键值对的映射关系和重建
    /// </summary>
      [Interface("")]
    public interface IDictionarySerializable
    {
        /// <summary>
        /// 从数据到字典
        /// </summary>
        /// <returns></returns>
       FreeDocument DictSerialize(Scenario scenario = Scenario.Database);

        /// <summary>
        /// 从字典到数据
        /// </summary>
        /// <param name="dicts">字典</param>
        /// <param name="scenario">应用场景</param>
        void DictDeserialize(IDictionary<string,object> docu, Scenario scenario = Scenario.Database);

    }
}
