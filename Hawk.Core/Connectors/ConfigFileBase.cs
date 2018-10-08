using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.Core.Utils.Logs;
namespace Hawk.Core.Connectors
{
    
    public abstract class ConfigFile : PropertyChangeNotifier, IConfigFile
    {
        #region Constants and Fields

        #endregion

        #region Constructors and Destructors

        protected ConfigFile()
        {
            Configs = new FreeDocument();
        }

        #endregion

        #region Properties

        private FreeDocument Configs { get; set; }

        [Browsable(false)]
        public string Name => AttributeHelper.GetCustomAttribute(GetType()).Name;

        [Browsable(false)]
        public virtual string SavePath { get; private set; }

        #endregion

        #region Public Methods

        private static readonly Dictionary<string, IConfigFile> configFiles = new Dictionary<string, IConfigFile>();

        public static IConfigFile Config => GetConfig();

        public static IConfigFile GetConfig(string name = null)
        {
            XFrmWorkAttribute first = PluginProvider.GetFirstPlugin(typeof (IConfigFile), name);

            if (first == null)
                return null;
            IConfigFile instance = null;
            if (configFiles.TryGetValue(first.Name, out instance))
            {
                return instance;
            }


            try
            {
                instance = PluginProvider.GetObjectInstance<IConfigFile>(first.Name);

                instance.ReadConfig(instance.SavePath);
            }
            catch (Exception ex)
            {
                XLogSys.Print.Warn(ex);
                if (instance == null)
                {
                    throw new Exception();
                }
                instance.RebuildConfig();
                instance.SaveConfig();
            }
            configFiles.Add(first.Name, instance);
            return instance;
        }

        public static T GetConfig<T>(string name = null) where T : class, IConfigFile, new()
        {
            if (name == null)
                name = AttributeHelper.GetCustomAttribute(typeof(T)).Name;
            IConfigFile instance = null;
            if (configFiles.TryGetValue(name, out instance))
            {
                return instance as T;
            }

            instance = PluginProvider.GetObjectInstance<IConfigFile>(name) ??
                       PluginProvider.GetObjectInstance(typeof (T)) as IConfigFile;
            if (instance == null)
                instance = new T() as IConfigFile;
            try
            {
                instance.ReadConfig(instance.SavePath);
            }
            catch (Exception ex)
            {
                instance.RebuildConfig();
                instance.SaveConfig();
            }
            configFiles.Add(name, instance);
            return instance as T;
        }

        #endregion

        #region Implemented Interfaces

        #region IConfigFile

        public virtual T Get<T>(string item)
        {
            return Configs.Get<T>(item);
        }

        public virtual int Increase(string name)
        {
            var v=Config.Get<int>(name);
            v++;
            Config.Set(name, v);
            return v;
        }
        public virtual void ReadConfig(string path = null)
        {
            if (path == null)
            {
                path = SavePath;
            }
            IFileConnector json = new FileConnectorXML();
            json.FileName = path;
            IDictionarySerializable da = json.ReadFile().FirstOrDefault();
            DictDeserialize(da.DictSerialize());
        }

        public virtual void RebuildConfig()
        {
            Configs = new FreeDocument();
        }

        public virtual void SaveConfig(string path = null)
        {
            if (path == null)
            {
                path = SavePath;
            }
            IFileConnector json = new FileConnectorXML();
            json.FileName = path;
            var Datas = new List<IFreeDocument> {DictSerialize()};
            json?.WriteAll(Datas);
        }

        /// <summary>
        ///     设置参数值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>如果值变化，返回true,反之返回false</returns>
        public virtual bool Set<T>(string key, T value)
        {
            var v = Configs.Get<T>(key);
            if (v == null)
            {
                Configs.SetValue(key, value);
                OnPropertyChanged(key);
                return true;
            }
            if (v.Equals(value) == false)
            {
                Configs.SetValue(key, value);
                OnPropertyChanged(key);
                return true;
            }
            return false;
        }

        public static T GetValue<T>(string item)
        {
            return Config.Get<T>(item);
        }

        public static bool SetValue<T>(string key, T value)
        {
            return Config.Set(key, value);
        }

        #endregion

        #region IDictionarySerializable

        public virtual void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            foreach (var dict in dicts)
            {
                Configs.SetValue(dict.Key, dict.Value);
            }
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            return Configs;
        }

        #endregion

        #endregion
    }
}