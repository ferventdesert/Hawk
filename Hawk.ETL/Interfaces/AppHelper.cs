using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Managements;
using Hawk.ETL.Process;
using System.Text.RegularExpressions;
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

        private static  string TemplateReplace(object key, ResourceDictionary dict)
        {
            if ((key is string) == false)
                return null;

            var value = dict[key];
            if (value == null)
                return null;
            string rvalue = value.ToString();
           var matchs= template.Matches(rvalue);
            foreach (Match match in matchs)
            {
               var key2= match.Groups[1].Value;
               var rvalue2 = TemplateReplace(key2, dict);
               rvalue= rvalue.Replace(match.Groups[0].Value, rvalue2);
                
            }
            dict[key] = rvalue;
            return rvalue;
        }
        private static Type lastType;
        private static PropertyInfo[] propertys;
        static  Regex template=new Regex(@"\{\{([^}]{1,20})\}\}");

        public static void LoadLanguage(string url = null)
        {
            ResourceDictionary langRd = null;

            if (url == null)
            {

                var config = ConfigFile.GetConfig<DataMiningConfig>().Get<string>("Language");
                if (string.IsNullOrEmpty(config))
                {
                    CultureInfo currentCultureInfo = CultureInfo.CurrentCulture;
                    var info = currentCultureInfo.Name;
                    url = @"Lang\" + info + ".xaml";

                }
                else
                {
                    url = config;
                }

            }

            try
            {
                langRd =
                    Application.LoadComponent(
                            new Uri(url, UriKind.Relative))
                        as ResourceDictionary;
                foreach (var key in langRd.Keys)
                {
                        TemplateReplace(key, langRd);

                }
                //ConfigFile.GetConfig().Set("Language", url);
                

            }
            catch (Exception e)
            {
            }

            if (langRd != null)
            {
                if (Application.Current.Resources.MergedDictionaries.Count > 0)
                {

                    Application.Current.Resources.MergedDictionaries.RemoveElementsNoReturn(d => d.Source != null && d.Source.ToString().Contains("DefaultLanguage.xaml"));
                }
                Application.Current.Resources.MergedDictionaries.Add(langRd);
            }


        }

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


        public static string Query(this IFreeDocument document, string input)
        {
            if (input == null)
                return null;
            var query = input.Trim();
            if (query.StartsWith("[") && query.EndsWith("]"))
            {
                var len = query.Length;
                query = query.Substring(1, len - 2);
                var result = document?[query];
                return result?.ToString();
            }
            if (query.StartsWith("{") && query.EndsWith("}"))
            {
                var len = query.Length;
                query = query.Substring(1, len - 2);
                var proj = MainDescription.MainFrm?.PluginDictionary["DataProcessManager"] as IProcessManager;
                if (proj != null)
                {
                    string value = "";
                    if (proj.CurrentProject.Parameters.TryGetValue(query, out value))
                        return value;

                }
            }
            return input;
        }

        public static async Task<T> RunBusyWork<T>(this IMainFrm manager, Func<T> func, string title = "系统正忙", string message = "正在处理长时间操作")
        {
            var dock = manager as IDockableManager;
            ControlExtended.UIInvoke(() => dock?.SetBusy(true, title, message));

            var item = await Task.Run(func);
            ControlExtended.UIInvoke(() => dock?.SetBusy(false));

            return item;
        }

        public static Func<List<string>> GetAllCrawlerNames(this IColumnProcess process)
        {
            var processManager = MainDescription.MainFrm.PluginDictionary["DataProcessManager"] as IProcessManager;
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
            var moduleName = (typeof(T) == typeof(SmartETLTool)) ? GlobalHelper.Get("key_201") : GlobalHelper.Get("smartcrawler_name");
            if (String.IsNullOrEmpty(name))
                return null;
            var process_name = process?.TypeName;
            if (process_name!=null&& String.IsNullOrEmpty(name))

            {
                XLogSys.Print.Error(String.Format(GlobalHelper.Get("key_203"),process_name));


                return default(T);
            }
            var processManager = MainDescription.MainFrm.PluginDictionary["DataProcessManager"] as IProcessManager;
            var module= processManager.GetModule<T>(name);
            if (module == null)
            {
                XLogSys.Print.Error(String.Format(GlobalHelper.Get("not_find_module"), name, moduleName, process_name));
                throw new NullReferenceException($"can't find a ETL Module named {name}");
            }
            return module;
        }

        public static T GetModule<T>(this IProcessManager processManager, string name) where T : class
        {
            var module =
               processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == name) as T;
            if (module != null)
            {
                return module;
            }

            var task = processManager.CurrentProject.Tasks.FirstOrDefault(d => d.Name == name);
            if (task == null)

            {

                return null;
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
                    if (str == GlobalHelper.Get("key_204"))
                        value = "NoTransform";
                    else if (str == GlobalHelper.Get("key_205"))
                        value = "List";
                    else if (str == GlobalHelper.Get("key_206"))
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
                        if (old_dict.TryGetValue(key, out value) && value!=null&&Boolean.TryParse(value.ToString(), out real_value))
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
            var processManager = MainDescription.MainFrm.PluginDictionary["DataProcessManager"] as IProcessManager;
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