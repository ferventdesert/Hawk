using System;
using System.Collections.Generic;
using System.Linq;
using Hawk.Core.Utils.Logs;

namespace Hawk.Core.Utils.Plugins
{
    /// <summary>
    /// 插件装载配置逻辑
    /// <remarks>该类主要为了解决不同系统对不同插件启动顺序的控制需求，例如，数据库通常在所有插件启动前启动，以获得应用运行所需的必备信息</remarks>
    /// </summary>
    public class PluginLoadControllor
    {

        public class BuildLogic
        {
            public BuildLogic(string name)
            {
                this.Name = name;
                Order = 1;
            }

            public BuildLogic(string name, int order)
            {
                this.Name = name;
                this.Order = order;
            }

            public BuildLogic()
            {
            }

            public string Name { get; set; }

            public int Order { get; set; }

        }
        public void SaveCurrentInfo()
        {
            CustomSerializer.Serialize<PluginLoadControllor>(this, FolderPosition + @"\" + FileName);
        }

        public static string FolderPosition;

        public static string FileName = "PluginLogicLog.xml";

        #region Constants and Fields

        private static PluginLoadControllor loadControllor;

        #endregion

        #region Properties

        public PluginLoadControllor()
        {
            BuildOrderLogic = new SerializableDictionary<string, List<BuildLogic>>();
            PluginNames = new List<InterfaceAttribute>();
            PluginDllNames = new List<string>();
        }


        /// <summary>
        /// 插件装载的逻辑
        /// </summary>
        public SerializableDictionary<string, List<BuildLogic>> BuildOrderLogic { get; set; }

        public List<InterfaceAttribute> PluginNames { get; set; }

        public List<string> PluginDllNames { get; set; }
        #endregion

        #region Public Methods

        public static bool IsNormalLoaded { get; private set; }



        public void AddBuildLogic<T>(IEnumerable<XFrmWorkAttribute> items)
        {
            var faceName = typeof(T).Name;
            if (BuildOrderLogic.ContainsKey(faceName))
            {
                BuildOrderLogic[faceName] = items.Select(d => new BuildLogic(d.Name)).ToList();
            }
            else
            {
                BuildOrderLogic.Add(faceName, items.Select(d => new BuildLogic(d.Name)).ToList());
            }
        }
        public void AddBuildLogic<T>(IEnumerable<BuildLogic> logics)
        {
            var faceName = typeof(T).Name;
            if (BuildOrderLogic.ContainsKey(faceName))
            {
                BuildOrderLogic[faceName] = logics.ToList();
            }
            else
            {
                BuildOrderLogic.Add(faceName, logics.ToList());
            }
        }
        public void AddBuildLogic<T>(Dictionary<string, int> logics)
        {
            var faceName = typeof(T).Name;
            if (BuildOrderLogic.ContainsKey(faceName))
            {
                BuildOrderLogic[faceName] = logics.Select(d => new BuildLogic(d.Key, d.Value)).ToList();
            }
            else
            {
                BuildOrderLogic.Add(faceName, logics.Select(d => new BuildLogic(d.Key, d.Value)).ToList());
            }
        }





        /// <summary>
        /// 尝试获取信息，若获取失败，返回一个false,并提供一个默认值，加入字典
        /// </summary>
        /// <param name="interfaceName">接口名称 </param>
        /// <param name="name">插件名称</param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool TryGetOrder(string interfaceName, string name, out int info)
        {
            List<BuildLogic> logics;
            if (this.BuildOrderLogic.TryGetValue(interfaceName, out logics))
            {
                var item = logics.FirstOrDefault(d => d.Name == name);
                if (item != null)
                {
                    info = item.Order;
                    return true;
                }
            }
            info = 1;
            return false;
        }
        public bool TryGetOrder<T>(string name, out int info)
        {
            List<BuildLogic> logics;
            if (this.BuildOrderLogic.TryGetValue(typeof(T).Name, out logics))
            {
                var item = logics.FirstOrDefault(d => d.Name == name);
                if (item != null)
                {
                    info = item.Order;
                    return true;
                }
            }
            info = 1;
            return false;
        }
        public int GetOrder<T>(string name)
        {
            List<BuildLogic> logics;
            if (this.BuildOrderLogic.TryGetValue(typeof(T).Name, out logics))
            {
                var item = logics.FirstOrDefault(d => d.Name == name);
                if (item != null)
                {
                    return item.Order;

                }
            }

            return 1;
        }
        public static PluginLoadControllor Instance
        {
            get
            {
                if (loadControllor == null)
                {
                    try
                    {
                        loadControllor = CustomSerializer.Deserialize<PluginLoadControllor>(FolderPosition + @"\" + FileName);

                        IsNormalLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        loadControllor = new PluginLoadControllor();
                        XLogSys.Print.Fatal(ex.Message + ",插件配置文件有错误，已经重建");
                        IsNormalLoaded = false;
                    }
                }
                return loadControllor;
            }
        }


        public List<BuildLogic> Get<T>()
        {
            List<BuildLogic> logics;
            if (this.BuildOrderLogic.TryGetValue(typeof(T).Name, out logics))
            {
                return logics;
            }

            return new List<BuildLogic>();
        }


        public int GetOrder(string interfaceName, string name)
        {
            List<BuildLogic> logics;
            if (this.BuildOrderLogic.TryGetValue(interfaceName, out logics))
            {
                var item = logics.FirstOrDefault(d => d.Name == name);
                if (item != null)
                {
                    return item.Order;

                }
            }

            return 1;
        }

        #endregion
    }
}