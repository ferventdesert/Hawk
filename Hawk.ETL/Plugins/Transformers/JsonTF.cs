using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Script.Serialization;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("转换为Json", "从字符串转换为json（数组或字典类型）")]
    public class JsonTF : TransformerBase
    {
        private readonly JavaScriptSerializer serialier;

        public JsonTF()
        {
            serialier = new JavaScriptSerializer();
            ScriptWorkMode =ScriptWorkMode.文档列表;
            OneOutput = false;
        }

        [DisplayName("工作模式")]
        public ScriptWorkMode ScriptWorkMode { get; set; }

        public override bool IsMultiYield => ScriptWorkMode == ScriptWorkMode.文档列表;

        public override object TransformData(IFreeDocument datas)
        {
            var item = datas[Column];
            if (item == null)
                return null;
            dynamic d = null;
            try
            {

                d = serialier.DeserializeObject(item.ToString());
            }
            catch (Exception ex)
            {
                
                SetValue(datas,ex.Message);
                // XLogSys.Print.Error(ex);
                return null;
            }
            if (ScriptWorkMode == ScriptWorkMode.单文档)
            {
                var newdoc = ScriptHelper.ToDocument(d) as FreeDocument;

                newdoc.DictCopyTo(datas);
            }
            else
            {
                SetValue(datas, d);
            }
       
            return null;

        }
  
        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var item = data[Column].ToString();
                if (string.IsNullOrEmpty(item))
                    continue;
                dynamic d = null;
                try
                {
                  
                    d = serialier.DeserializeObject(item.ToString());
                }
                catch (Exception ex)
                {
                  //  XLogSys.Print.Error(ex);
                    continue;
                }

            
                      
                        foreach (var item2 in ScriptHelper.ToDocuments(d))
                        {
                            var item3 = item2 as FreeDocument;
                            yield return item3.MergeQuery(data, NewColumn);
                        }
            }
        }
    }
}