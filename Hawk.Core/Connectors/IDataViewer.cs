using System.Collections.Generic;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    [Interface("IDataViewer")]
    public interface IDataViewer
    {
        object SetCurrentView(IEnumerable<IFreeDocument> datas);
        /// <summary>
        /// 指示是否可编辑
        /// </summary>
        bool IsEditable { get; }
    }
}
