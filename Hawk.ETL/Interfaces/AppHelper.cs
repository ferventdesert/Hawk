using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Managements;
using Hawk.ETL.Process;

namespace Hawk.ETL.Interfaces
{
    public static class AppHelper
    {
        private static readonly List<Type> SpecialTypes = new List<Type>
        {
            typeof (ExtendSelector),
            typeof (ExtendSelector<string>),
            typeof (TextEditSelector)
        };

        private static Type lastType;
        private static PropertyInfo[] propertys;
        private static readonly string _static_name = "模块管理";



        public static IEnumerable<T> CountOutput<T>(this IEnumerable<T> documents, AnalyzeItem analyzer=null)
        {
            return documents.Select(d =>
            {
                if (analyzer != null)
                    ++analyzer.Output;
                return d;
            });
        }

        public static IEnumerable<T> CountInput<T>(this IEnumerable<T> documents, AnalyzeItem analyzer = null)

        {
            if(documents==null)
                yield break;
            foreach (var document in documents)
            {
                if (analyzer != null) ++analyzer.Input;
                yield return document;
            }
        }


      

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
            return delegate
            {
                var item=
                 processManager.CurrentProcessCollections.Where(d => d is SmartCrawler)
                    .Select(d => d.Name)
                    .Concat(
                        processManager.CurrentProject.Tasks.Where(d => d.ProcessToDo["Type"].ToString() == "SmartCrawler")
                            .Select(d => d.Name)).
                    Distinct().ToList();

                return item;
            };
        }

        public static T GetModule<T>(this IColumnProcess process, string name) where T : class
        {
            var moduleName = (typeof(T) == typeof(SmartETLTool)) ? "数据清洗" : "网页采集器";
            if (string.IsNullOrEmpty(name))
                return null;
            var process_name = process?.TypeName;
            if (process_name!=null&& string.IsNullOrEmpty(name))

            {
                XLogSys.Print.Error($"您没有填写“{process_name}”的对应参数。");


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
               
                XLogSys.Print.Error($"没有找到名称为'{name}'的{moduleName}，请检查“{process_name}”是否填写错误");
                throw new NullReferenceException($"can't find a ETL Module named {name}");
            }

            ControlExtended.UIInvoke(() => { task.Load(false); });
            module =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == name) as T;
            return module;
        }

        /// <summary>
        /// 高版本配置向低版本兼容
        /// </summary>
        /// <param name="new_dict"></param>
        /// <param name="old_dict"></param>
        public static void  ConfigVersionConverter(FreeDocument new_dict, IDictionary<string, object> old_dict)
        {
            var new_dic = new FreeDocument();
            foreach (var item in old_dict)
            {
                object value = null;
                if (item.Value == null)
                    continue;
                if (item.Key == "ScriptWorkMode")
                {
                    var str = item.Value.ToString();
                    if (str == "不进行转换")
                        value = "NoTransform";
                    else if (str == "文档列表")
                        value = "List";
                    else if (str == "单文档")
                    {
                        value = "One";
                    }


                    if (value != null)
                    {
                        new_dic.Add(item.Key, value);
                    }
                }


                else
                {
                    var key = new_dict.FirstOrDefault(d => d.Value != null && d.Value.GetType() == typeof(ScriptWorkMode)).Key;
                    if (key != null && key == item.Key)
                    {
                        bool real_value;
                        if (old_dict.TryGetValue(key, out value) && value!=null&&bool.TryParse(value.ToString(), out real_value))
                        {
                            var new_value = real_value ? ScriptWorkMode.List : ScriptWorkMode.One;
                            new_dic.Add(key, new_value);
                        }
                    }
                }
            }

            if (new_dic.Any())
                foreach (var o in new_dic)
                {
                    old_dict[o.Key] = o.Value;
                }


        }
        public static Func<List<string>> GetAllETLNames(this IColumnProcess process)
        {
            var processManager = MainDescription.MainFrm.PluginDictionary[_static_name] as IProcessManager;
            return () => processManager.CurrentProcessCollections.Where(d => d is SmartETLTool)
                .Select(d => d.Name)
                 .Concat(
                        processManager.CurrentProject.Tasks.Where(d => d.ProcessToDo["Type"].ToString() == "SmartETLTool")
                            .Select(d => d.Name)).
                Distinct().ToList();
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