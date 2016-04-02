using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Interfaces
{
   public static class AppHelper
    {
        public static async Task<T> RunBusyWork<T>(this IMainFrm manager, Func<T> func, string title = "系统正忙",
        string message = "正在处理长时间操作")
        {
            var dock = manager as IDockableManager;
            ControlExtended.UIInvoke(() => dock?.SetBusy(true, title, message));

            T item = await Task.Run(func);
            ControlExtended.UIInvoke(() => dock?.SetBusy(false));

            return item;
        }

    }
}
