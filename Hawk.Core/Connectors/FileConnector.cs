using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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

        public virtual bool ShouldConfig => false;

        public Type DataType { get; set; }


        public virtual string ExtentFileName => ".txt";

        public string FileName { get; set; }
        public EncodingType EncodingType { get; set; }

        public Dictionary<string, string> PropertyNames { get; set; }

        #endregion

        #region Public Methods

        public static string GetCollectionString(IEnumerable<IFreeDocument> datas, string format = "xml")
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

        public static string GetItemString(IFreeDocument datas, string format = "xml")
        {
            return GetCollectionString(new List<IFreeDocument> { datas });
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


        public static IFreeDocument ReadDataFile(string path)
        {
            IFileConnector json = SmartGetExport(path);
            json.FileName = path;
            IFreeDocument r = json.ReadFile().LastOrDefault();
            return r;
        }

        public static List<IFreeDocument> ReadGroupDataFile(string path)
        {
            IFileConnector json = SmartGetExport(path);

            json.FileName = path;
            return json.ReadFile().ToList();
        }

        public static void SaveDataFile(string filename, IEnumerable<IFreeDocument> items)
        {
            IFileConnector json = SmartGetExport(filename);
            json.FileName = filename;

            json.WriteAll(items);
        }

        public static void SaveDataFile(string filename, IFreeDocument item)
        {
            IFileConnector json = SmartGetExport(filename);
            json.FileName = filename;
            var datas = new List<IFreeDocument> { item };
            json.WriteAll(datas);
        }

        #endregion

        #region Implemented Interfaces

        #region IFileConnector

        public virtual bool IsVirtual
        {
            get { return false; }
        }


        public abstract IEnumerable<IFreeDocument> ReadFile(Action<int> alreadyGetSize = null);
    

        public virtual string GetString(IEnumerable<IFreeDocument> datas)
        {
            return "不支持此功能";
        }

        #endregion

        #endregion

        #region Methods

        public abstract IEnumerable<IFreeDocument> WriteData(IEnumerable<IFreeDocument> data);

    

        #endregion

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
           var dict=new FreeDocument();
            dict.Add("Type", this.GetType().Name);
            dict.Add("Encoding", EncodingType);
            return dict;
        }

        public virtual  void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            EncodingType = docu.Set("Encoding", EncodingType);
        }
    }
}