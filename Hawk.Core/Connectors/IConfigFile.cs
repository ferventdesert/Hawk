using System.ComponentModel;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    [Interface( "配置文件接口")]
    public interface IConfigFile : INotifyPropertyChanged, IDictionarySerializable
    {
        #region Properties

        string SavePath { get; }

        #endregion

        string Name { get; }

        #region Indexers

        T Get<T>(string item);

        bool Set<T>(string key, T value);
        
        #endregion

        #region Public Methods


        void RebuildConfig();
        void ReadConfig(string path = null);

        void SaveConfig(string path = null);

        #endregion
    }
}