using System;
using System.Collections.Generic;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
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
        [LocalizedDescription("script_mode_list")]
        List,
        [LocalizedDescription("script_mode_one")]
        One,

        [LocalizedDescription("script_mode_none")]
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
        [LocalizedDescription("SortByColumn")]
        SortByColumn,
        [LocalizedDescription("SortByRow")]
        SortByRow,
        [LocalizedDescription("SortByScore")]
        SortByScore,
        [LocalizedDescription("SortByArea")]
        SortByArea,
      
    }
}