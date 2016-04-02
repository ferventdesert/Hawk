using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
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



        [DisplayName("编码")]
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

      

        //[XFrmWork("文件写入操作", "写入文件中的文本")]
        //public class FileContentExecutor : DataExecutorBase
        //{
        //    [DisplayName("文件路径")]
        //    public string FilePath { get; set; }

        //    public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        //    {

        //        var connect = FileConnector.SmartGetExport(FilePath);
        //        return connect.WriteData(documents).Select(d => d.DictSerialize());

        //    }
        //}



    }
    [XFrmWork("写入文件文本", "写入文件中的文本")]
    public class WriteFileTextTF : DataExecutorBase
    {
        private bool shouldUpdate;
        private string _folderPath;



        [DisplayName("编码")]
        public EncodingType EncodingType { get; set; }



        public string Path { get; set; }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            foreach (var document in documents)
            {
                string item = document[Column].ToString();
                var mypath = document.Query(Path);
                File.WriteAllText(mypath, item, AttributeHelper.GetEncoding(EncodingType));
                yield return document;
            }


        }
    }


}