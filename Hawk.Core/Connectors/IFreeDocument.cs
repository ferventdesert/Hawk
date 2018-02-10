using System;
using System.Collections.Generic;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    /// <summary>
    /// 自由格式文档
    /// </summary>
    public interface IFreeDocument : IDictionarySerializable, IDictionary<string, object>, IComparable
    {
        #region Properties

        IDictionary<string, object> DataItems { get; set; }

        IEnumerable<string> PropertyNames { get; }

        #endregion
    }
    /// <summary>
    /// 转换器脚本运行方式
    /// </summary>
    public enum ScriptWorkMode
    {
        List,
        One,
        NoTransform,
    }


    /// <summary>
    /// Web请求运行方式
    /// </summary>
    public enum RequestMode 
    {
        HttpClient,
        SuperMode,
        Browser,
    }



    public enum SelectorFormat
    {
        XPath,
        CssSelecor,

    }
    /// <summary>
    /// 爬虫获取数据运行方式
    /// </summary>
    public enum CrawlType
    {
        InnerText,
        InnerHtml,
        OuterHtml,

    }

    /// <summary>
    /// 手气不错排序方式
    /// </summary>
    public enum SortMethod
    {
        按列数排序,
        按行数排序,
        按分数排序,
        按面积排序
    }
}