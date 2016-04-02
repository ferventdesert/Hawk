using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    public abstract class FileConnector : IFileConnector
    {
        #region Constructors and Destructors

        public static Dictionary<XFrmWorkAttribute, string> ConnectorDictionary;

        static FileConnector()
        {
            ConnectorDictionary = new Dictionary<XFrmWorkAttribute, string>();
            foreach (XFrmWorkAttribute item in PluginProvider.GetPluginCollection(typeof(IFileConnector)))
            {
                var ins = PluginProvider.GetObjectInstance(item.MyType) as IFileConnector;
                ConnectorDictionary.Add(item, ins.ExtentFileName);
            }
        }

        protected FileConnector()
        {
            PropertyNames = new Dictionary<string, string>();
            DataType = typeof(FreeDocument);
        }

        #endregion

        #region Properties

        public virtual bool ShouldConfig
        {
            get { return false; }
        }

        public Type DataType { get; set; }


        public virtual string ExtentFileName => ".txt";

        public string FileName { get; set; }

        public Dictionary<string, string> PropertyNames { get; set; }

        #endregion

        #region Public Methods

        public static string GetCollectionString(IEnumerable<IDictionarySerializable> datas, string format = "xml")
        {
            IFileConnector connector = null;
            switch (format)
            {
                case "xml":
                    connector = new FileConnectorXML();
                    break;

                case "json":
                    connector = new FileConnectorXML();
                    break;
            }
            return connector.GetString(datas);
            ;
        }

        public static string GetItemString(IDictionarySerializable datas, string format = "xml")
        {
            return GetCollectionString(new List<IDictionarySerializable> { datas });
        }

        public static string GetDataFilter()
        {
            var sb = new StringBuilder();
            sb.Append("All Files|*.*|");
            foreach (var v in ConnectorDictionary)
            {
                sb.Append(v.Key.Name);

                sb.Append($"|*{v.Value}|");
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }


        public static string SmartGetExpotStr(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;


            string extent = Path.GetExtension(fileName);
            FileInfo inf;
            if (File.Exists(fileName))
            {
                inf = new FileInfo(fileName);
                long size = inf.Length;
            }
            extent = extent.ToLower();
            string pluginName = null;

            foreach (var item in ConnectorDictionary)
            {
                if (item.Value == extent)
                {
                    return item.Key.Name;
                }
            }

            return pluginName;
        }

        public static IFileConnector SmartGetExport(string fileName)
        {
            string item = SmartGetExpotStr(fileName);
            if (item == null)
                return null;

            var con = PluginProvider.GetObjectInstance<IFileConnector>(item);
            con.FileName = fileName;
            return con;
        }


        public static IDictionarySerializable ReadDataFile(string path)
        {
            IFileConnector json = SmartGetExport(path);
            json.FileName = path;
            IDictionarySerializable r = json.ReadFile().LastOrDefault();
            return r;
        }

        public static List<IDictionarySerializable> ReadGroupDataFile(string path)
        {
            IFileConnector json = SmartGetExport(path);

            json.FileName = path;
            return json.ReadFile().ToList();
        }

        public static void SaveDataFile(string filename, IEnumerable<IDictionarySerializable> items)
        {
            IFileConnector json = SmartGetExport(filename);
            json.FileName = filename;

            json.WriteAll(items);
        }

        public static void SaveDataFile(string filename, IDictionarySerializable item)
        {
            IFileConnector json = SmartGetExport(filename);
            json.FileName = filename;
            var datas = new List<IDictionarySerializable> { item };
            json.WriteAll(datas);
        }

        #endregion

        #region Implemented Interfaces

        #region IFileConnector

        public virtual bool IsVirtual
        {
            get { return false; }
        }


        public abstract IEnumerable<IDictionarySerializable> ReadFile(Action<int> alreadyGetSize = null);
    

        public virtual string GetString(IEnumerable<IDictionarySerializable> datas)
        {
            return "不支持此功能";
        }

        #endregion

        #endregion

        #region Methods

        public abstract IEnumerable<IDictionarySerializable> WriteData(IEnumerable<IDictionarySerializable> data);

    

        #endregion
    }
}