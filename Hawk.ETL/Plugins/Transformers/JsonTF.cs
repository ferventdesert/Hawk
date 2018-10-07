using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Process;
using HtmlAgilityPack;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("JsonTF", "JsonTF_desc")]
    public class JsonTF : TransformerBase
    {
        private readonly JavaScriptSerializer serialier;
        private string lastData;

        public JsonTF()
        {
            serialier = new JavaScriptSerializer();
            ScriptWorkMode = ScriptWorkMode.NoTransform;
            OneOutput = false;
        }

        [LocalizedDisplayName("key_188")]
        [LocalizedDescription("etl_script_mode")]
        public ScriptWorkMode ScriptWorkMode { get; set; }


        [Browsable(false)]
        public override string KeyConfig => ScriptWorkMode.ToString();

        private SmartCrawler selector;
        private bool crawlerEnabled = false;
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
                IsMultiYield = ScriptWorkMode == ScriptWorkMode.List;
            lastData = null;
            return base.Init(docus);
        }

        public override object TransformData(IFreeDocument datas)
        {
            var item = datas[Column];
            if (item == null || string.IsNullOrWhiteSpace(item.ToString()))
                return null;
            
            dynamic d = null;
            try
            {
                d = serialier.DeserializeObject(item.ToString());
            }
            catch (Exception ex)
            {
                SetValue(datas, ex.Message);
                // XLogSys.Print.Error(ex);
                return null;
            }
            if (ScriptWorkMode == ScriptWorkMode.One)
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

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas,AnalyzeItem analyzer)
        {
            foreach (var data in datas)
            {
                var item = data[Column].ToString();
                if (string.IsNullOrEmpty(item))
                    continue;
                var itemstr = item;
                lastData = itemstr;
                if (crawlerEnabled)
                {
                    bool isrealjson;
                    var html = JavaScriptAnalyzer.Json2XML(itemstr, out isrealjson, true);
                    if (isrealjson)
                    {
                        HtmlDocument htmldoc = null;
                        var doc = selector.CrawlHtmlData(html, out htmldoc);
                        foreach (var item3 in doc)
                        {
                            yield return item3.MergeQuery(data, NewColumn);

                        }

                    }
                    continue;
                }
                dynamic d = null;
                try
                {
                    d = serialier.DeserializeObject(itemstr);
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