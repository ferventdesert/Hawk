using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils.Plugins;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using EncodingType = Hawk.Core.Utils.EncodingType;

namespace Hawk.Core.Connectors
{
    public abstract class FileConnector : IFileConnector
    {
        #region Constructors and Destructors

        public static Dictionary<XFrmWorkAttribute, string> ConnectorDictionary
        {
            get
            {
               var dict= new Dictionary<XFrmWorkAttribute, string>();
                foreach (XFrmWorkAttribute item in PluginProvider.GetPluginCollection(typeof(IFileConnector)))
                {
                    var ins = PluginProvider.GetObjectInstance(item.MyType) as IFileConnector;
                    dict.Add(item, ins.ExtentFileName);
                }
                return dict;
            }
        }

        static FileConnector()
        {
           
        }

        protected FileConnector()
        {
            PropertyNames = new Dictionary<string, string>();
        }

        #endregion

        #region Properties

        [Browsable(false)]
        public virtual bool ShouldConfig => false;


        [LocalizedDisplayName("key_38")]
        public virtual string ExtentFileName => ".txt";

        [Browsable(false)]
        public string FileName { get; set; }

        [LocalizedDisplayName("key_39")]
        public EncodingType EncodingType { get; set; }


        [Browsable(false)]
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

                sb.Append($"|*{v.Value.Split(' ')[0]}|");
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


        public static FreeDocument ReadDataFile(string path)
        {
            IFileConnector json = SmartGetExport(path);
            json.FileName = path;
            FreeDocument r = json.ReadFile().LastOrDefault();
            return r;
        }

        public static List<FreeDocument> ReadGroupDataFile(string path)
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

        public virtual bool IsVirtual => false;


        public abstract IEnumerable<FreeDocument> ReadFile(Action<int> alreadyGetSize = null);



        public virtual string GetString(IEnumerable<IFreeDocument> datas)
        {
            return GlobalHelper.Get("key_40");
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