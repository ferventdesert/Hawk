using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Transformers;
using AttributeHelper = Hawk.Core.Utils.AttributeHelper;
using EncodingType = Hawk.Core.Utils.EncodingType;

namespace Hawk.ETL.Plugins.Generators
{


    [XFrmWork("读取文件文本", "获取文件中的文本")]
    public class ReadFileTextTF : TransformerBase
    {
        private BuffHelper<string> buffHelper = new BuffHelper<string>(50);



        [LocalizedDisplayName("编码")]
        public EncodingType EncodingType { get; set; }

        public override object TransformData(IFreeDocument datas)
        {
            string item = datas[Column].ToString();
            var res = buffHelper.Get(item);
            if (res != null)
                return res;
            else
            {

                var content = File.ReadAllText(item, AttributeHelper.GetEncoding(EncodingType));
                buffHelper.Set(item, content);
                return content;
            }
        }



      



    }



    [XFrmWork("读取文件数据", "从文件中读取内容")]
    public class ReadFileTF :TransformerBase 
    {

        public ReadFileTF()
        {
            ConnectorSelector = new ExtendSelector<XFrmWorkAttribute>(PluginProvider.GetPluginCollection(typeof(IFileConnector)));

            ConnectorSelector.SelectChanged += (s, e) =>
            {
                if(ConnectorSelector.SelectItem==null)
                    return;
                Connector = PluginProvider.GetObjectInstance<IFileConnector>(ConnectorSelector.SelectItem.Name);
                OnPropertyChanged("Connector");
            };
            Enabled = false;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            FreeDocument dict = base.DictSerialize();

            if (Connector != null)
            {
                dict.SetValue("Connector", Connector.DictSerialize());
            }
            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserialize(docu);
            if (docu.ContainsKey("Connector"))
            {
                List<XFrmWorkAttribute> coll = PluginProvider.GetPluginCollection(typeof(IFileConnector));
                object doc2 = docu["Connector"];
                var p = doc2 as IDictionary<string, object>;
                object name = p["Type"];
                if (name != null)
                {
                    var result=
                        coll.FirstOrDefault(d => d.MyType.Name == name.ToString());
                    ConnectorSelector.SelectItem = result;

                    if (Connector != null)
                    {
                       ( Connector as IDictionarySerializable).DictDeserialize(p);
                    }
                }
            }
        }


        [LocalizedDisplayName("文件格式")]
        public ExtendSelector<XFrmWorkAttribute> ConnectorSelector { get; set; }

        [LocalizedDisplayName(("连接器配置"))]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public IFileConnector Connector { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
           
            IsMultiYield = true;
           return base.Init(docus);
        }
        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            if(Connector==null)
                yield break;
            
            foreach (var data in datas)
            {
                var path = data[Column].ToString();
                Connector.FileName = path;
                foreach (var item in Connector.ReadFile())
                {
                    yield return item.MergeQuery(data, NewColumn);
                }
            }
          
        }

       
    }


    [XFrmWork("写入文件文本", "写入文件中的文本")]
    public class WriteFileTextTF : DataExecutorBase
    {
        private bool shouldUpdate;
        private string _folderPath;



        [LocalizedDisplayName("编码")]
        public EncodingType EncodingType { get; set; }


        [LocalizedDisplayName("要写入文件的完整路径")]
        public string Path { get; set; }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            foreach (var document in documents)
            {
                string item = document[Column].ToString();
                var mypath = document.Query(Path);
                var path = document.Query(mypath);
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                var folder = directoryInfo.Parent;
                if (folder == null)
                    continue;
                if (!folder.Exists)
                {

                    folder.Create();
                }
                var url = document[Column].ToString();
                if (string.IsNullOrEmpty(url))
                    continue;
                File.WriteAllText(mypath, item, AttributeHelper.GetEncoding(EncodingType));
                yield return document;
            }


        }
    }


}