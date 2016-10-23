using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Plugins.Transformers;
using HtmlAgilityPack;

namespace Hawk.ETL.Crawlers
{
    [XFrmWork("XPath筛选器", "通过XPath选取html中的子节点文档")]
    public class XPathTF : TransformerBase
    {
        [LocalizedDisplayName("XPath路径")]
        public string XPath { get; set; }

        [LocalizedDisplayName("获取多个数据")]
        [LocalizedDescription("当要获取符合XPath语法的多个结果时，勾选该选项")]
        public bool IsManyData { get; set; }

        [LocalizedDisplayName("获取正文")]
        [LocalizedDescription("勾选此项后，会自动提取新闻正文，XPath路径可为空")]
        public bool GetText { get; set; }


        [LocalizedDisplayName("获取正文HTML")]
        [LocalizedDescription("勾选此项后，会自动提取新闻正文的HTML，XPath路径可为空")]
        public bool GetTextHtml { get; set; }


        [LocalizedDisplayName("获取节点数量")]
        [LocalizedDescription("获取符合XPath语法的节点的数量")]
        public bool GetCount { get; set; }


        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var item = data[Column];
                var docu = new HtmlDocument();

                docu.LoadHtml(item.ToString());
                var p2 = docu.DocumentNode.SelectNodes(XPath);
                if (p2 == null)
                    continue;
                foreach (var node in p2)
                {
                    var doc = new FreeDocument();
                    doc.Add("Text", node.GetNodeText());
                    doc.Add("HTML", node.InnerHtml);
                    doc.Add("OHTML", node.OuterHtml);
                    yield return doc.MergeQuery(data, NewColumn);
                }
            }
        }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            IsMultiYield = IsManyData;
            return base.Init(docus);
        }

        public override object TransformData(IFreeDocument document)
        {
            var item = document[Column];

            if (item is IFreeDocument)
            {
                return (item as IFreeDocument).GetDataFromXPath(XPath);
            }
            var docu = new HtmlDocument();

            docu.LoadHtml(item.ToString());
            if (GetText)
            {
                var path = docu.DocumentNode.GetTextNode();
                var textnode = docu.DocumentNode.SelectSingleNode(path);
                if (textnode != null)
                    return textnode.GetNodeText();
            }
            if (GetTextHtml)
            {
                var path = docu.DocumentNode.GetTextNode();
                var textnode = docu.DocumentNode.SelectSingleNode(path);
                if (textnode != null)
                    return textnode.InnerHtml;
            }
            if (GetCount)
            {
                var textnode = docu.DocumentNode.SelectNodes(XPath);
                return textnode.Count;
            }

            var res = docu.DocumentNode.GetDataFromXPath(document.Query(XPath));
            if (res == null)
            {
            }
            return res;
        }
    }


    [XFrmWork("门类枚举", "要拖入HTML文本列，输入XPath,该功能可以将页面中的门类，用Cross模式组合起来，适合于爬虫无法抓取全部页面，但可以按分类抓取的情况")]
    public class XPathTF2 : TransformerBase
    {
        private List<string> xpaths;


        public XPathTF2()
        {
            Script = "";
            HasHtml = true;
          
        }
        [DisplayName("多个XPath路径")]
        [Description("代表多个门类的XPath路径，一行一条")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }


        [DisplayName("获取html")]
        public bool HasHtml { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            var datas = Script.Split('\n');
            IsMultiYield = true;
            xpaths = datas.Select(d => d.Trim()).Where(d=>!string.IsNullOrEmpty(d)).ToList();
            return true;
        }

        private IEnumerable<IFreeDocument> Get( HtmlDocument docu, IEnumerable<IFreeDocument>source, string xpath, int count)
        {
            HtmlNodeCollection nodes;
            try
            {
                 nodes = docu.DocumentNode.SelectNodes(xpath);
            }
            catch (Exception ex)
            {
               XLogSys.Print.Warn("XPath表达式错误: "+ xpath);
                return source;
            }
            if (nodes.Count == 0)
            {
                XLogSys.Print.Warn("XPath表达式: "+xpath+ "获取的节点数量为0");
                return source;
            }
            var new_docs = nodes.Select(node =>
            {
                var doc = new FreeDocument();
                doc.Add("xp_text_" + count, node.GetNodeText());
                if (HasHtml)
                {
                    doc.Add("xp_html_" + count, node.InnerHtml);

                    doc.Add("xp_ohtml_" + count, node.OuterHtml);
                }
                return doc;
            });
            return new_docs.Cross(source);

        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var item = data[Column];
                var docu = new HtmlDocument();

                docu.LoadHtml(item.ToString());
                var d = new FreeDocument();
                d.MergeQuery(data, NewColumn);
                IEnumerable<IFreeDocument> source = new List<IFreeDocument>() {d};
                for (int index = 0; index < xpaths.Count; index++)
                {
                    var xpath = xpaths[index];
                    source = Get(docu, source, xpath, index);
                  
                }
                foreach (var r in source)
                {
                    yield return r;
                }
            }
            yield break;
        }


    }
}