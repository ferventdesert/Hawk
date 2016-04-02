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
}