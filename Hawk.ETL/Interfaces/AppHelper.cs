using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Process;

namespace Hawk.ETL.Interfaces
{
    public static class AppHelper
    {
        private static readonly List<Type> SpecialTypes = new List<Type>
        {
            typeof (ExtendSelector),
            typeof (TextEditSelector)
        };

        private static Type lastType;
        private static PropertyInfo[] propertys;
        private static readonly string _static_name = "模块管理";

        public static async Task<T> RunBusyWork<T>(this IMainFrm manager, Func<T> func, string title = "系统正忙",
            string message = "正在处理长时间操作")
        {
            var dock = manager as IDockableManager;
            ControlExtended.UIInvoke(() => dock?.SetBusy(true, title, message));

            var item = await Task.Run(func);
            ControlExtended.UIInvoke(() => dock?.SetBusy(false));

            return item;
        }

        public static Func<List<string>> GetAllCrawlerNames(this IColumnProcess process)
        {
            var processManager = MainDescription.MainFrm.PluginDictionary[_static_name] as IProcessManager;
            return () => processManager.CurrentProcessCollections.Where(d => d is SmartCrawler)
                .Select(d => d.Name)
                .ToList();
        }

        public static T GetModule<T>(this IColumnProcess process, string name) where T : class
        {
            var moduleName = (typeof(T) == typeof(SmartETLTool)) ? "数据清洗" : "网页采集器";
            if (string.IsNullOrEmpty(name))

            {
                XLogSys.Print.Error($"您没有填写“{process.TypeName}”的对应参数。");


                return default(T);
            }
            var processManager = MainDescription.MainFrm.PluginDictionary[_static_name] as IProcessManager;
            var module =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == name) as T;
            if (module != null)
            {
                return module;
            }

            var task = processManager.CurrentProject.Tasks.FirstOrDefault(d => d.Name == name);
            if (task == null)

            {
               
                XLogSys.Print.Error($"没有找到名称为'{name}'的{moduleName}，请检查“{process.TypeName}”是否填写错误");
                throw new NullReferenceException($"can't find a ETL Module named {name}");
            }

            ControlExtended.UIInvoke(() => { task.Load(false); });
            module =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == name) as T;
            return module;
        }

        public static Func<List<string>> GetAllETLNames(this IColumnProcess process)
        {
            var processManager = MainDescription.MainFrm.PluginDictionary[_static_name] as IProcessManager;
            return () => processManager.CurrentProcessCollections.Where(d => d is SmartETLTool)
                .Select(d => d.Name)
                .ToList();
        }

        public static void UnsafeDictDeserializePlus(this object item, IDictionary<string, object> dict)
        {
            item.UnsafeDictDeserialize(dict);
            var type = item.GetType();
            if (type != lastType)
            {
                propertys =
                    type.GetProperties().Where(
                        d => SpecialTypes.Contains(d.PropertyType)).ToArray();
            }
            lastType = type;

            foreach (var propertyInfo in propertys)
            {
                dynamic old_item = propertyInfo.GetValue(item, null);
                string old_value = null;
                if (old_item != null)
                {
                    old_value = old_item.SelectItem;
                }
                old_item.SelectItem = dict.Set(propertyInfo.Name, old_value);
            }
        }

        public static FreeDocument UnsafeDictSerializePlus(this object item)

        {
            var doc = item.UnsafeDictSerialize();
            var type = item.GetType();
            if (type != lastType)
            {
                propertys =
                    type.GetProperties().Where(
                        d => SpecialTypes.Contains(d.PropertyType)).ToArray();
            }
            lastType = type;

            foreach (var propertyInfo in propertys)
            {
                dynamic v = propertyInfo.GetValue(item, null);
                if (v != null)
                    doc.Add(propertyInfo.Name, v.SelectItem);
            }
            return doc;
        }
    }
}