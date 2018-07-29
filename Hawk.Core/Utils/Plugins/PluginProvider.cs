using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Hawk.Core.Utils.Logs;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    ///     单例模式提供的插件搜索器
    /// </summary>
    public class PluginProvider
    {
        #region Constructors and Destructors

        private PluginProvider()
        {
        }

        #endregion

        private static XFrmWorkAttribute getXFrmWorkAttribute(Type type, Type interfaceType)
        {
            var item = AttributeHelper.GetAttributes<XFrmWorkAttribute>(type).FirstOrDefault();
            if (item != null)
                return item;
            var desc = AttributeHelper.GetAttributes<DescriptionAttribute>(type).FirstOrDefault();
            var des = desc == null ? type.ToString() : desc.Description;
            return new XFrmWorkAttribute(type.Name, des) 
            {
                MyType = type
            };
        }

        public static XFrmWorkAttribute GetAttribute(Type dataType, Type interfaceType)
        {
            List<XFrmWorkAttribute> plugins = null;
            if (PluginDictionary.TryGetValue(interfaceType, out plugins))
            {
                try
                {
                    var attr = plugins.First(d => d.MyType == dataType);
                    return attr;
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error(ex);
                }
            }
            return null;
        }

        /// <summary>
        ///     获取某插件在插件目录中的索引号
        /// </summary>
        /// <param name="interfaceName">接口名称</param>
        /// <param name="className">类名</param>
        /// <returns></returns>
        public static int GetObjectIndex(Type interfaceName, Type className)
        {
            foreach (var rc in GetPluginCollection(interfaceName).Where(rc => rc.MyType == className))
            {
                return GetPluginCollection(interfaceName).IndexOf(rc);
            }
            throw new Exception(GlobalHelper.Get("key_152")+GlobalHelper.Get("key_153") +className+GlobalHelper.Get("key_154")+interfaceName);
        }

        public static object GetObjectInstance(Type pluginType)
        {
            object res = null;
            try
            {
                res = Activator.CreateInstance(pluginType);
            }
            catch (Exception ex)
            {
                throw new Exception(ex + pluginType.Name);
            }
            return res;
        }

        /// <summary>
        ///     创建一个带参数的实例
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object GetObjectInstance(Type pluginType, params object[] args)
        {
            return Activator.CreateInstance(pluginType, args);
        }

        /// <summary>
        ///     通过插件名称获取当前插件的实例
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object GetObjectInstance(string pluginType, string args)
        {
            var type = PluginDictionary.Keys.FirstOrDefault(d => d.Name == pluginType);
            if (type == null)
                return null;
            var coll = PluginDictionary[type].FirstOrDefault(d => d.Name == args);
            if (coll == null)
                return null;
            return Activator.CreateInstance(coll.MyType);
        }

        /// <summary>
        ///     查找一个不带接口约束的实例
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object GetObjectInstance(string pluginName)
        {
            XFrmWorkAttribute attr = null;
            foreach (var r in PluginDictionary)
            {
                foreach (var t in r.Value)
                {
                    if (t.Name == pluginName)
                    {
                        attr = t;
                        break;
                    }
                }
            }
            if (attr == null)
                return null;
            return GetObjectInstance(attr.MyType);
        }

        /// <summary>
        ///     获取接口中固定索引类型的实例
        /// </summary>
        /// <param name="interfaceName">接口名称</param>
        /// <param name="index">索引号</param>
        /// <returns>实例化的引用</returns>
        public static object GetObjectInstance(Type interfaceName, int index)
        {
            var collection = GetPluginCollection(interfaceName);
            if (index > collection.Count - 1)
                return null;

            return Activator.CreateInstance(collection[index].MyType);
        }

        /// <summary>
        ///     获取接口中固定索引类型的实例
        /// </summary>
        /// <returns>实例化的引用</returns>
        public static T GetObjectByType<T>(string name) where T : class
        {
            XFrmWorkAttribute type;
            type = string.IsNullOrEmpty(name)
                ? GetPluginCollection(typeof(T)).FirstOrDefault()
                : GetPluginCollection(typeof(T)).FirstOrDefault(d => d.MyType.Name == name);


            if (type == null)
            {
                return null;
            }
            return GetObjectInstance(type.MyType) as T;
        }
        /// <summary>
        ///     获取接口中固定索引类型的实例
        /// </summary>
        /// <returns>实例化的引用</returns>
        public static T GetObjectInstance<T>(string name) where T : class
        {
            XFrmWorkAttribute type;
            type = string.IsNullOrEmpty(name)
                ? GetPluginCollection(typeof (T)).FirstOrDefault()
                : GetPluginCollection(typeof (T)).FirstOrDefault(d => d.Name == name);


            if (type == null)
            {
                return null;
            }
            return GetObjectInstance(type.MyType) as T;
        }

        /// <summary>
        ///     获取接口中固定索引类型的实例
        /// </summary>
        /// <returns>实例化的引用</returns>
        public static T SearchObjectInstance<T>(string name) where T : class
        {
            XFrmWorkAttribute type;
            type = string.IsNullOrEmpty(name)
                ? GetPluginCollection(typeof (T)).FirstOrDefault()
                : GetPluginCollection(typeof (T)).FirstOrDefault(d => d.Name.ToLower().Contains(name.ToLower()));

            if (type == null)
            {
                return null;
            }
            return GetObjectInstance(type.MyType) as T;
        }

        public static XFrmWorkAttribute GetPluginAttribute(Type interfaceName, Type pluginType)
        {
            var plugins = GetPluginCollection(interfaceName);
            return plugins.FirstOrDefault(d => (d.MyType == pluginType));
        }

        public static XFrmWorkAttribute GetPluginAttribute(Type pluginType)
        {
            return PluginDictionary.SelectMany(r => r.Value).FirstOrDefault(t => t.MyType == pluginType);
        }

        /// <summary>
        ///     获取某程序集的接口列表
        /// </summary>
        /// <param name="typeInterface"></param>
        /// <param name="myAssembly"></param>
        /// <returns></returns>
        public static List<XFrmWorkAttribute> GetPluginCollection(
            Type typeInterface, Assembly myAssembly)
        {
            return GetPluginCollection(typeInterface, myAssembly, false);
        }

        public static IList<string> GetPluginNames(Type interfaceName)
        {
            var plugins = GetPluginCollection(interfaceName);
            return plugins.Select(d => d.Name).ToList();
        }

        public static XFrmWorkAttribute GetPluginAttribute<T>(string name)
        {
            var attrs = GetPluginCollection(typeof (T));
            return attrs.FirstOrDefault(d => d.Name == name);
        }

        public static List<XFrmWorkAttribute> GetPluginCollection(
            Type typeInterface, Assembly myAssembly, bool isAbstractRequired)
        {
            if (PluginDictionary.ContainsKey(typeInterface))
            {
                return PluginDictionary[typeInterface].ToList();
            }
            var tc = new List<XFrmWorkAttribute>();

            var types = myAssembly.GetTypes();
            foreach (var type in types)
            {
                if (type.GetInterface(typeInterface.ToString()) == null) continue;
                if (!isAbstractRequired && type.IsAbstract)
                {
                    continue;
                }

                // Iterate through all the Attributes for each method.
                foreach (Attribute attr in
                    type.GetCustomAttributes(typeof (XFrmWorkAttribute), false))
                {
                    var attr2 = attr as XFrmWorkAttribute;
                    attr2.MyType = type;
                    tc.Add(attr2);
                }
            }
            PluginDictionary.Add(typeInterface, tc);
            return tc;
        }

        public static XFrmWorkAttribute GetPlugin(string pluginName)
        {
            return
                PluginDictionary.Select(item => item.Value.FirstOrDefault(d => d.Name == pluginName))
                    .FirstOrDefault(p => p != null);
        }

        public static List<XFrmWorkAttribute> GetPluginCollection(string interfaceName)
        {
            var type = PluginDictionary.Keys.FirstOrDefault(d => d.Name == interfaceName);
            if (type == null)
                return new List<XFrmWorkAttribute>();
            return GetPluginCollection(type);
        }

        public static XFrmWorkAttribute GetFirstPlugin(Type interfaceName, string name = null)
        {
            var plugins = GetPluginCollection(interfaceName);
            XFrmWorkAttribute first = null;
            first = name != null
                ? plugins.Where(d => d.MyType.Name == name).OrderByDescending(d => d.GroupName).FirstOrDefault()
                : plugins.OrderByDescending(d => d.GroupName).FirstOrDefault();
            return first;
        }
        public static XFrmWorkAttribute GetFirstPluginAttr(Type interfaceName, string name = null)
        {
            var plugins = GetPluginCollection(interfaceName);
            XFrmWorkAttribute first = null;
            first = name != null
                ? plugins.Where(d => d.Name == name).OrderByDescending(d => d.GroupName).FirstOrDefault()
                : plugins.OrderByDescending(d => d.GroupName).FirstOrDefault();
            return first;
        }

        public static List<XFrmWorkAttribute> GetPluginCollection(Type typeInterface)
        {
            if (PluginDictionary.ContainsKey(typeInterface))
            {
                return PluginDictionary[typeInterface].ToList();
            }
            var attrs = new List<XFrmWorkAttribute>();
            foreach (var item in PluginDictionary)
            {
                foreach (var attribute in item.Value)
                {
                    if (attribute.MyType.GetInterface(typeInterface.Name) != null &&
                        attrs.FirstOrDefault(d => d.Name == attribute.Name) == null)
                        attrs.Add(attribute);
                }
            }
            PluginDictionary.Add(typeInterface, attrs);

           
            return attrs ;
        }

        public static void SaveConfigFile()
        {
            PluginLoadControllor.Instance.SaveCurrentInfo();
        }

        #region Constants and Fields

        /// <summary>
        ///     插件字典
        /// </summary>
        public static Dictionary<Type, List<XFrmWorkAttribute>> PluginDictionary =
            new Dictionary<Type, List<XFrmWorkAttribute>>();

        private static readonly object Lockobj = new object();

        #endregion

        #region Properties

        public static string MainConfigFolder
        {
            get { return PluginLoadControllor.FolderPosition; }
            set { PluginLoadControllor.FolderPosition = value; }
        }

        /// <summary>
        ///     按顺序查询的搜索文件夹
        /// </summary>
        public static string[] OrderedSearchFolder { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     初始化插件系统
        /// </summary>
        /// <param name="pluginDir"></param>
        public static void Init(IEnumerable<string> pluginDir, string configDir)
        {
            OrderedSearchFolder = pluginDir.ToArray();
            MainConfigFolder = configDir;
            GetAllPluginName(false);

            GetAllPlugins(false);
        }

        /// <summary>
        ///     获取所有的插件接口契约名称
        /// </summary>
        /// <param name="isRecursiveDirectory">是否对目录实现递归搜索 </param>
        public static void GetAllPluginName(bool isRecursiveDirectory)
        {
            var loader = PluginLoadControllor.Instance;


            if (PluginLoadControllor.IsNormalLoaded)
            {
                return;
            }
            XLogSys.Print.Debug(GlobalHelper.Get("key_155"));
            var dllFile = new List<string>();

            foreach (var folder in OrderedSearchFolder)
            {
                dllFile.AddRange(
                    from file in Directory.GetFileSystemEntries(folder)
                    //若无缓存文件，获取目录下全部的dll文件执行搜索
                    where Path.GetExtension(file) == ".dll"
                    select file);
            }
            dllFile = dllFile.Distinct().ToList();

            foreach (var file in dllFile)
            {
                XLogSys.Print.Debug(GlobalHelper.Get("key_156") + file);
                Type[] types;
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(file);
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error(file + "插件加载错误\n" + ex.Message);
                    continue;
                }

                try
                {
                    types = assembly.GetTypes();
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error(file + "插件获取类型错误\n" + ex.Message);
                    continue;
                }
                foreach (var type in types)
                {
                    if (type.IsInterface == false)
                    {
                        continue;
                    }
                    // Iterate through all the Attributes for each method.
                    foreach (Attribute attr in
                        type.GetCustomAttributes(typeof (InterfaceAttribute), false))
                    {
                        var iattr = attr as InterfaceAttribute;
                        iattr.Name = type.Name;
                        loader.PluginNames.Add(iattr);
                    }
                }
            }
        }


        public static void LoadOrderedPlugins<T>(Action<XFrmWorkAttribute> action, int minPriority, int maxPriority)
        {
            var plugins = GetPluginCollection(typeof (T));
            if (PluginLoadControllor.IsNormalLoaded == false)
            {
                PluginLoadControllor.Instance.AddBuildLogic<T>(plugins);
            }


            var orderdPlugins =
                from plugin in plugins
                let order = PluginLoadControllor.Instance.GetOrder<T>(plugin.Name)
                where order >= minPriority && order <= maxPriority
                orderby order descending
                select plugin;

            foreach (var xFrmWorkAttribute in orderdPlugins)
            {
                action(xFrmWorkAttribute);
            }
        }

        /// <summary>
        ///     获取所有插件
        /// </summary>
        /// <param name="isRecursiveDirectory">是否进行目录递归搜索</param>
        public static void GetAllPlugins(bool isRecursiveDirectory)
        {
            var loader = PluginLoadControllor.Instance;

            var allDllFileList = new List<string>();

            foreach (var s in OrderedSearchFolder)
            {
                allDllFileList.AddRange(
                    from file in Directory.GetFileSystemEntries(s) where Path.GetExtension(file) == ".dll" select file);
            }
            allDllFileList = allDllFileList.Distinct().ToList();

            Type[] types = null;
            var dllPluginFils = new List<string>(); //最终要进行处理的的dll文件

            if (PluginLoadControllor.IsNormalLoaded == false)
            {
                dllPluginFils = allDllFileList;
            }
            else
            {
                dllPluginFils.AddRange(
                    loader.PluginDllNames.Select(
                        pluginDllName =>
                            allDllFileList.FirstOrDefault(d => Path.GetFileNameWithoutExtension(d) == pluginDllName))
                        .Where(file => file != null));
                //否则将插件文件名称拼接为完整的文件路径
            }

            {
                foreach (var file in dllPluginFils)
                {
                    Assembly assembly;
                    try
                    {
                        var name =
                            AppDomain.CurrentDomain.GetAssemblies()
                                .FirstOrDefault(d2 => d2.GetName().Name == Path.GetFileNameWithoutExtension(file));
                        if (name != null)
                            assembly = name;
                        else
                        {
                            assembly = Assembly.LoadFrom(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        XLogSys.Print.Error(ex);
                        continue;
                    }

                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (Exception ex)
                    {
                        XLogSys.Print.Error(ex);
                        continue;
                    }
                    if (types == null)
                        continue;
                    foreach (var type in types)
                    {
                        if (type.IsInterface)
                        {
                            continue;
                        }
                        foreach (var interfacename in loader.PluginNames) //对该Type，依次查看是否实现了插件名称列表中的接口
                        {
                            var interfaceType = type.GetInterface(interfacename.Name);
                            if (interfaceType != null)
                            {
                                var interfaceName = interfacename.Name;
                                // Iterate through all the Attributes for each method.
                                try
                                {
                                    foreach (Attribute attr in
                                        type.GetCustomAttributes(typeof (XFrmWorkAttribute), false))
                                        //获取该插件的XFrmWorkAttribute标识
                                    {
                                        var attr2 = attr as XFrmWorkAttribute;
                                        attr2.MyType = type; //将其类型赋值给XFrmWorkAttribute

                                        List<XFrmWorkAttribute> pluginInfo = null; //保存到插件字典当中
                                        if (PluginDictionary.TryGetValue(interfaceType, out pluginInfo))
                                        {
                                            pluginInfo.Add(attr2); //若插件字典中已包含了该interfaceType的键，则直接添加
                                        }
                                        else
                                        {
                                            var collection = new List<XFrmWorkAttribute> {attr2};
                                            lock (Lockobj)
                                            {
                                                if (!PluginDictionary.ContainsKey(interfaceType))
                                                {
                                                    PluginDictionary.Add(interfaceType, collection); //否则新建一项并添加之
                                                }
                                            }
                                        }

                                        var file2 = Path.GetFileNameWithoutExtension(file);
                                        if (!loader.PluginDllNames.Contains(file2)) //若插件文件列表中不包含此文件则添加到文件目录中
                                        {
                                            loader.PluginDllNames.Add(file2);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ex.HelpLink = type.ToString();
                                    throw ex;
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes());
            var dicts =
                loader.PluginNames.ToList();
            //针对dll搜索策略的插件
            if (!dicts.Any())
                return;
            foreach (var type in allTypes)
            {
                //  string[] interfaces = type.GetInterfaces().Select(d => d.Name).ToArray();
                if (type.IsAbstract || type.IsInterface)
                    continue;
                foreach (var item in dicts)
                {
                    List<XFrmWorkAttribute> attrs = null;
                    var iType = type.GetInterface(item.Name);
                    if (iType == null)
                        continue;
                    var ignore =
                        AttributeHelper.GetAttributes<XFrmWorkIgnore>(type);
                          
                    if (ignore.Any())
                        return; //如果发现被标记忽略且名字一致的插件，则忽略它

                    if (PluginDictionary.TryGetValue(iType, out attrs))
                    {
                        if (attrs.FirstOrDefault(d => d.MyType == type) == null)
                            attrs.Add(getXFrmWorkAttribute(type, iType));
                    }
                    else
                    {
                        attrs = new List<XFrmWorkAttribute> {getXFrmWorkAttribute(type, iType)};
                        PluginDictionary.Add(iType, attrs);
                    }
                }
            }
        }
    }
}