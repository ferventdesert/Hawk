using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
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


        [LocalizedDisplayName("获取节点数量")]
        [LocalizedDescription("获取符合XPath语法的节点的数量")]
        public bool GetCount { get; set; }

        [LocalizedDisplayName("插入空行")]
        [LocalizedDescription("勾选此项后，每个页面后会插入一个空行")]
        public bool IsInsertNull { get; set; }
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
                if(IsInsertNull)
                    yield return new FreeDocument();
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
            if (GetCount)
            {
                var textnode = docu.DocumentNode.SelectNodes(XPath);
                return textnode.Count;
            }

            return docu.DocumentNode.GetDataFromXPath(document.Query(XPath));
        }
    }

   
}