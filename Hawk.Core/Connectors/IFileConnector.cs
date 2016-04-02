using System;
using System.Collections.Generic;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    [Interface( "文件导入导出基本接口" )]
    public interface IFileConnector
    {
        #region Properties

        /// <summary>
        /// 数据类型
        /// </summary>
        Type DataType { get; set; }
 
        /// <summary>
        /// 扩展文件名
        /// </summary>
        string ExtentFileName { get; }
        /// <summary>
        /// 要读写的文件名
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// 可选的数据属性和名称映射
        /// </summary>
        Dictionary<string,string> PropertyNames { get; set; }

        #endregion

      

        #region Public Methods
      

         /// <summary>
         /// 迭代式文件操作
         /// </summary>
         /// <param name="path"></param>
         IEnumerable<IDictionarySerializable> WriteData(IEnumerable<IDictionarySerializable> datas );
        /// <summary>
        /// 读文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
         IEnumerable<IDictionarySerializable> ReadFile(Action<int> alreadyGetSize=null);

        /// <summary>
        /// 获取得到的文本
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        string GetString(IEnumerable<IDictionarySerializable> datas);

        #endregion
    }
}