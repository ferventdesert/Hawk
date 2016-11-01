using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Xml;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using HtmlAgilityPack;

namespace Hawk.ETL.Crawlers
{
    public enum ListType
    {
        List,
        One
    }

    /// <summary>
    ///     Main class to search paths on HTML
    /// </summary>
    public static class XPathAnalyzer
    {
      

        private static readonly Regex indexRegex = new Regex(@"\w+\[\d+\]");
        public static double PM25 = 2.4;
        private static Regex titleRegex = new Regex(@"h\d");
        private static readonly string[] ignores = {"style", "script", "comment"};

        /// <summary>
        ///     判断a1和a2是否是有效的文本
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns></returns>
        public static bool IsVaildText(string a1, string a2)
        {
            if (a1 == a2)
                return false;
            a1 = a1?.Trim();
            a2 = a2?.Trim();
            return (string.IsNullOrEmpty(a1) || string.IsNullOrEmpty(a2)) == false;
        }

        /// <summary>
        ///     分析 url 字符串中的参数信息
        /// </summary>
        /// <param name="url">输入的 URL</param>
        /// <param name="baseUrl">输出 URL 的基础部分</param>
        /// <param name="nvc">输出分析后得到的 (参数名,参数值) 的集合</param>
        public static Dictionary<string, string> ParseUrl(string url)
        {
            if (url == null)
                throw new ArgumentNullException("url");
            var dict = new Dictionary<string, string>();
            var baseUrl = "";
            if (url == "")
                return null;
            var questionMarkIndex = url.IndexOf('?');
            if (questionMarkIndex == -1)
            {
                baseUrl = url;
                return null;
            }
            baseUrl = url.Substring(0, questionMarkIndex);
            if (questionMarkIndex == url.Length - 1)
                return null;
            var ps = url.Substring(questionMarkIndex + 1);
            // 开始分析参数对  
            var re = new Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", RegexOptions.Compiled);
            var mc = re.Matches(ps);
            foreach (Match m in mc)
            {
                dict.Add(m.Result("$2").ToLower(), m.Result("$3"));
            }
            return dict;
        }

        public static List<string> GetDiffXPath(HtmlNode node1, HtmlNode node2, bool hasAttribute = false)
        {
            var list = new List<string>();
            GetDiff(node1, node2, list, hasAttribute);
            return list;
        }

        /// <summary>
        ///     尝试从页面中找到翻页控件，并返回翻页控件的根节点，目前还有问题，没有实用性
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static HtmlNode GetPageListURL(this HtmlNode doc)
        {
            return GetPageListURLInner(doc);
        }

        private static HtmlNode GetPageListURLInner(HtmlNode doc)
        {
            var index = 0;
            var rIndex = 0;
            for (index = 0; index < doc.ChildNodes.Count; index++)
            {
                var r = doc.ChildNodes[index];
                var node = GetPageListURLInner(r);

                if (node != null) return node;
                if (r.ChildNodes.Count != 0)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(r.InnerText) == false && r.InnerText.Trim() == (index + 1).ToString())
                    rIndex++;
            }
            if (doc.ChildNodes.Count >= 3 && rIndex > doc.ChildNodes.Count/2)
            {
                return doc;
            }
            return null;
        }

        /// <summary>
        ///     递归查询两个节点中值不一样的XPATH
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="paths"></param>
        /// <param name="hasAttribute"></param>
        /// <returns></returns>
        private static bool GetDiff(HtmlNode node1, HtmlNode node2, List<string> paths, bool hasAttribute = false)
        {
            if (node2 == null && node1 != null && node1.XPath.Contains("#") == false)
            {
                // paths.Add(node1.XPath);
                return false;
            }

            if (node1 == null && node2 != null && node2.XPath.Contains("#") == false)
            {
                // paths.Add(node2.XPath);
                return false;
            }
            if (node1 == null || node2 == null)
                return false;
            if (paths.Count > 0 && (node1.XPath.Contains("#") || node2.XPath.Contains("#")))
                return false;
            var childHasDiff = false;
            for (var index = 0; index < Math.Max(node1.ChildNodes.Count, node2.ChildNodes.Count); index++)
            {
                var child1 = node1.ChildNodes.Count > index ? node1.ChildNodes[index] : null;
                var child2 = node2.ChildNodes.Count > index ? node2.ChildNodes[index] : null;

                childHasDiff |= GetDiff(child1, child2, paths);
            }
            if (childHasDiff == false && IsVaildText(node1.InnerText, node2.InnerText) &&
                node1.XPath.Contains("#") == false)
            {
                paths.Add(node1.XPath);
                childHasDiff = true;
            }


            if (hasAttribute)
            {
                var attributs = node1.Attributes.Select(d => d.Name).ToList();
                attributs.AddRange(node2.Attributes.Select(d => d.Name));
                var att = attributs.Distinct().ToList();
                paths.AddRange(from item in att
                    let child1 = node1.Attributes[item]
                    let child2 = node2.Attributes[item]
                    where child1 != child2
                    select (child1 != null ? child1.XPath : null) ?? (child2 != null ? child2.XPath : null));
            }
            return childHasDiff;
        }

        /// <summary>
        ///     从XPath获取数据
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="path"></param>
        /// <param name="ishtml"></param>
        /// <returns></returns>
        public static string GetDataFromXPath(this HtmlNode doc, string path, bool ishtml = false)
        {
            if (!string.IsNullOrEmpty(path))
            {
                HtmlNode p2 = null;
                try
                {
                    p2 = doc.SelectSingleNode(path);
                }
                catch (Exception ex)
                {
                }

                if (p2 == null)
                    return null;

                var paths = path.Split('/');
                var last = paths[paths.Length - 1];
                if (last.Any() && last.Contains("@") && last.Contains("[1]")) //标签数据
                {
                    var name = XPath.GetAttributeName(last.Split('@', '[')[1]);
                    if (p2.HasAttributes)
                    {
                        var a = p2.Attributes.FirstOrDefault(d => d.Name == name);
                        return a?.Value.Trim();
                    }
                }
                else if (ishtml)
                    return p2.InnerHtml;
                else
                    return p2.GetNodeText();
            }
            return null;
        }

        private static string SearchPropertyName(this HtmlNode node, List<CrawlItem> crawlItems)
        {
            var attrkey = "class";
            var value = "";
            //难道就不考虑提个特征么？通过SVM的特征来确定标题
            if (node.Attributes.FirstOrDefault(d => d.Name == attrkey) != null)
            {
                var attr = node.Attributes[attrkey].Value;
                if (crawlItems.FirstOrDefault(d => d.Name == attr) == null)
                {
                    value = attr;
                }
                if (node.ParentNode.Attributes.FirstOrDefault(d => d.Name == attrkey) != null)
                {
                    var parentattr = node.ParentNode.Attributes[attrkey].Value;
                    value = parentattr + '_' + attr;
                }

                value = value.Replace(' ', '_');
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(node.Name) )
                {
                    var attr = node.Name; 
                    if (crawlItems.FirstOrDefault(d => d.Name == attr) == null)
                    {
                        value = attr;
                    }
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }
            return null;
            //if(node.Name == "td") //数据列
            //{
            //    var p = node.ParentNode;
            //    var index = p.ChildNodes.IndexOf(node);
            //    var title= node.ParentNode.ParentNode.FirstChild.ChildNodes[index];
            //    return title.InnerText;
            //}
            //if(titleRegex.IsMatch(node.Name))
            //{
            //    return node.Name.Replace("h", "标题");
            //}
            //if (node.PreviousSibling != null)
            //{
            //    var pre = node.PreviousSibling;
            //    if (pre.InnerText.EndsWith(":") || pre.InnerText.EndsWith("："))
            //    {
            //        var property=
            //            pre.InnerText.Split(new char[] {':', '：'}, StringSplitOptions.RemoveEmptyEntries)
            //                .FirstOrDefault();
            //        return property;
            //    }
            //}
            //return null;
            //if (node.Attributes.Contains("id"))
            //{

            //}
            //if (node.Attributes.Contains("title"))
            //{
            //    if (node.Attributes["title"].Value != node.InnerText)
            //        return node.Attributes["title"].Value;
            //}
        }

        private static bool ListEqual(List<string> a, List<string> b)
        {
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        public static bool IsAncestor(this HtmlNode node, HtmlNode root)
        {
            while (node != null)
            {
                if (node == root)
                    return true;
                node = node.ParentNode;
            }
            return false;
        }

        /// <summary>
        ///     find different text and return it name and xpath
        /// </summary>
        /// <param name="isAttrEnabled">是否抓取标签中的数据</param>
        /// <returns></returns>
        private static bool GetDiffNodes(List<HtmlNode> nodes, List<CrawlItem> result, List<List<string>> buffers,
            bool isAttrEnabled)
        {
            var isChildContainInfo = false;
            var node1 = nodes.First();
            var node2 = nodes[1];
            if (node1.ChildNodes.Count == 1 && node1.ChildNodes[0].NodeType == HtmlNodeType.Text)
            {
                var row = nodes.Select(d => d.SelectSingleNode(d.XPath))
                    .Where(d => d != null).Select(d => d.InnerText.Trim()).ToList();
                if (row.Any(d => CompareString(d, node1.InnerText) == false))
                {
                    var name = node1.SearchPropertyName(result) ?? "属性" + result.Count;
                    if (buffers.Any(d => ListEqual(d, row)))
                        return true;
                    var crawlItem = new CrawlItem
                    {
                        Name = name,
                        SampleData1 = node1.InnerText,
                        XPath = result.Count%2 == 0 ? node1.XPath : node2.XPath
                    };
                    result.Add(crawlItem);
                    buffers.Add(row);
                    return true;
                }
                return false;
            }
            foreach (var nodechild1 in node1.ChildNodes)
            {
                if (nodechild1.XPath.Contains("#"))
                    continue;

                var path = XPath.TakeOff(nodechild1.XPath, node1.XPath);
                bool fail = false;
                
                var nodechild2 =
                    nodes.Select(d =>
                    {
                        if (fail)
                            return null;
                        try
                        {
                            var node = d.SelectSingleNode(d.XPath + path);
                            return node;
                        }
                        catch (Exception)
                        {

                            fail = true;
                            return null;
                        }
                    
                    }).Where(d => d != null&&fail==false).ToList();
                if (nodechild2.Count<2||fail)
                    continue;

                isChildContainInfo |= GetDiffNodes(nodechild2, result, buffers, isAttrEnabled);
            }


            if (isAttrEnabled == false)
            {
                return isChildContainInfo;
            }
            foreach (var attribute in node1.Attributes)
            {
                var attr1 = attribute.Value;
                ;
                var row = nodes.Select(d => d.SelectSingleNode(d.XPath))
                    .Where(d => d != null)
                    .Where(d => d.Attributes.Contains(attribute.Name))
                    .Select(d => d.Attributes[attribute.Name].Value)
                    .ToList();
                if (row.Any(d => CompareString(d, attr1) == false))
                {
                    if (buffers.Any(d => ListEqual(d, row)))
                        return isChildContainInfo;
                    var name = node1.SearchPropertyName(result);
                    if (name != null)
                        name += '_' + attribute.Name;
                    else
                    {
                        name = "属性" + result.Count;
                    }

                    var craw = new CrawlItem {Name = name, SampleData1 = attr1, XPath = attribute.XPath};
                    result.Add(craw);
                }
            }
            return isChildContainInfo;
        }

        private static bool CompareString(string text1, string text2)
        {
            text1 = text1.Trim();
            text2 = text2.Trim();
            return text1 == text2;
        }

        /// <summary>
        ///     通过在FreeDocument中查询找到对应的XPath
        /// </summary>
        /// <param name="freeDocument">要查找的文档</param>
        /// <param name="value">检索资料</param>
        /// <returns></returns>
        public static string SearchXPath(this IFreeDocument freeDocument, string value)
        {
            var r = SearchXPathInner(freeDocument, value);
            if (r == null)
                return null;
            if (r.StartsWith("/") == false)
                return '/' + r;
            return r;
        }

        /// <summary>
        ///     对未知类型元素内部进行查询的XPath
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        /// <param name="isKey">为false则进行值查询，否则进行键查询</param>
        /// <returns></returns>
        private static string SearchXPathInner(this object item, string value)
        {
            var xpath = "";
            if (item is IDictionary<string, object>)
            {
                var doc = item as IDictionary<string, object>;
                foreach (var r in doc)
                {
                    var path = r.Value?.SearchXPathInner(value);
                    if (path != null)
                    {
                        xpath += '/' + r.Key + path;

                        return xpath;
                    }
                }
            }
            else if (item is IList)
            {
                var list = item as IList;
                for (var i = 0; i < list.Count; i++)
                {
                    var path = list[i].SearchXPathInner(value);
                    if (path != null)
                    {
                        if (path != "")

                            xpath += $"[{i}]/" + path;
                        else
                        {
                            xpath += $"[{i}]";
                        }
                        return xpath;
                    }
                }
            }
            else
            {
                if (item.ToString().Contains(value))
                    return "";
            }


            return null;
        }

        /// <summary>
        ///     对freeDocument实现XPath选择器
        /// </summary>
        /// <param name="freeDocument">要查找的文档</param>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public static object GetDataFromXPath(this IDictionary<string, object> freeDocument, string xpath)
        {
            var words = xpath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            object child = freeDocument;
            for (var i = 0; i < words.Length; i++)
            {
                var doc = child as IDictionary<string, object>;
                if (doc != null)
                {
                    var name = words[i];
                    var index = -1;
                    if (indexRegex.IsMatch(words[i])) //实现数组索引
                    {
                        var items = words[i].Split("[]".ToCharArray());
                        name = items[0];
                        int.TryParse(items[1], out index);
                        if (index == -1) //若产生类似/[2]/[3]的表达，默认找其Children
                        {
                            name = "Children";
                        }
                    }
                    if (doc.TryGetValue(name, out child) == false)
                    {
                        return null;
                    }
                    if (index != -1)
                    {
                        var list = child as IList;
                        if (list != null) child = list[index];
                    }
                }
            }
            return child;
        }

        public static Dictionary<string, double> GetTableRootProbability(IList<HtmlNode> nodes, bool haschild)
        {
            var dict = new Dictionary<string, double>();
            GetTableRootProbability(nodes, dict, haschild);
            dict = dict.OrderByDescending(d => d.Value).ToDictionary(d => d.Key, d => d.Value);
            return dict;
        }

        public static double Variance(this double[] values)
        {
            return Variance(values, values.Average());
        }

        public static double Variance(this double[] values, double mean)
        {
            return Variance(values, mean, true);
        }

        public static double Variance(this double[] values, double mean, bool unbiased = true)
        {
            var variance = 0.0;

            for (var i = 0; i < values.Length; i++)
            {
                var x = values[i] - mean;
                variance += x*x;
            }

            if (unbiased)
            {
                // Sample variance
                return variance/(values.Length - 1);
            }
            // Population variance
            return variance/values.Length;
        }

        /// <summary>
        ///     计算可能是列表根节点的概率，同时将值保存在dict中。
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dict"></param>
        private static void GetTableRootProbability(IList<HtmlNode> nodes, Dictionary<string, double> dict,
            bool haschild)
        {
            if (nodes.Count == 0)
                return;
            var node = nodes[0];
            var xpath =XPath. RemoveFinalNum(node.XPath);
            if (haschild)
            {
                foreach (var htmlNode in nodes)
                {
                    GetTableRootProbability(htmlNode.ChildNodes.Where(d => d.Name.Contains("#") == false).ToList(), dict,
                        haschild);
                }
            }

            var avanode = nodes.ToList();
            if (avanode.Count < 3)
                return;
            if (avanode.Count(d => d.Name == avanode[1].Name) < avanode.Count*0.7)
                return;

            var childCount = (double) avanode.Count;

            var childCounts = avanode.Select(d => (double) d.ChildNodes.Count).ToArray();
            var v = childCounts.Variance();
            //TODO: 此处需要一个更好的手段，因为有效节点往往是间隔的
            if (v > 100)
            {
               return;
            }

            var leafCount = avanode.First().GetLeafNodeCount();
            var value = childCount*PM25 + leafCount;


            dict.SetValue(xpath, value);
        }

        public static string GetTextNode(this HtmlNode node)
        {
            var para = new ParaClass();

            node.GetTextRootProbability(para);

            return XPath.SubXPath(para.Path, -2);
        }

        private static void GetTextRootProbability(this HtmlNode node,
            ParaClass paraClass)
        {
            if (ignores.Any(ignore => node.Name.ToLower().Contains(ignore)))
                return;
            if (node.ChildNodes.Count == 0)
            {
                var text = node.InnerText.Trim();
                if (string.IsNullOrEmpty(text) == false)
                {
                    var textlen = text.Length;
                    if (textlen > paraClass.tLen)
                    {
                        paraClass.tLen = textlen;
                        paraClass.Path = node.XPath;
                    }
                }
            }
            else
            {
                foreach (var childNode in node.ChildNodes)
                {
                    GetTextRootProbability(childNode, paraClass);
                }
            }
        }

        public static string GetNodeText(this HtmlNode node)
        {
            if (node == null)
            {
                return "";
            }
            if (node.NodeType == HtmlNodeType.Comment)
                return "";
            if (ignores.Any(ignore => node.Name.ToLower().Contains(ignore)))
                return "";
            var sb = new StringBuilder();
            if (node.NodeType == HtmlNodeType.Text)
            {
                sb.Append(node.InnerText.Trim());
            }
            foreach (var childNode in node.ChildNodes)
            {
                var text = childNode.GetNodeText();
                if (text.Length > 0)
                    sb.Append(" ");
                sb.Append(childNode.GetNodeText());
            }
            return sb.ToString();
        }

        public static HtmlDocument GetDocumentFromURL(string url, EncodingType encoding = EncodingType.Unknown)
        {
            var httpitem = new HttpItem();
            httpitem.URL = url;

            httpitem.Encoding = encoding;
            var helper = new HttpHelper();
            HttpStatusCode code;

            var doc = new HtmlDocument();
            var result = helper.GetHtml(httpitem, out code);
            if (!HttpHelper.IsSuccess(code))
                return doc;
            doc.LoadHtml(result);
            return doc;
        }

        /// <summary>
        ///     递归获取一个节点的所有叶子节点的数量
        /// </summary>
        /// <param name="node"></param>
        /// <param name="count"></param>
        private static void GetLeafNodeCount(this HtmlNode node, ref int count)
        {
            if (node == null)
                return;
            if (node.ChildNodes.Count == 0)
            {
                count++;
            }
            foreach (var r in node.ChildNodes)
            {
                r.GetLeafNodeCount(ref count);
            }
        }

        /// <summary>
        ///     递归获取一个节点的所有叶子节点的数量
        /// </summary>
        /// <param name="node"></param>
        /// <param name="count"></param>
        public static int GetLeafNodeCount(this HtmlNode node)
        {
            var leafNodeCount = 0;
            node.GetLeafNodeCount(ref leafNodeCount);
            return leafNodeCount;
        }

        /// <summary>
        ///     从批量集合中获取数据
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="crawItem"></param>
        /// <param name="root"></param>
        /// <param name="document"></param>
        public static void GetDataFromXPath(this HtmlDocument doc, CrawlItem crawItem, string root,
            IFreeDocument document)
        {
            var result = doc.DocumentNode.GetDataFromXPath( XPath.TakeOff(crawItem.XPath,root),
                crawItem.IsHTML);


            if (result != null)
                document.SetValue(crawItem.Name, result);
        }

        /// <summary>
        ///     从批量集合中获取数据
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="crawItem"></param>
        /// <param name="shortv"></param>
        /// <param name="document"></param>
        public static void GetDataFromXPath(this HtmlDocument doc, CrawlItem crawItem, IFreeDocument document)
        {
            var result = doc.DocumentNode.GetDataFromXPath(crawItem.XPath, crawItem.IsHTML);


            if (result != null)
                document.SetValue(crawItem.Name, result);
        }

        /// <summary>
        ///     判断两个字符串是不是一样的
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsContainKeyword(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a))
                return false;
            var item =
                a.Split(new[] {'\t', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .ToList();
            if (item.Count == 0)
                return false;

            return item.Any(d => d.Contains(b));
        }

        /// <summary>
        ///     搜索并递归获取Html Dom的XPath
        /// </summary>
        /// <param name="node">要处理的节点</param>
        /// <param name="keyword">关键词</param>
        /// <param name="hasAttr">是否需要搜索attr标记</param>
        public static IEnumerable<string> SearchXPath(this HtmlNode node, string keyword, Func<bool> hasAttr)
        {
            if (node == null || keyword == null)
                yield break;


            foreach (var item in node.ChildNodes)
            {
                var res = SearchXPath(item, keyword, hasAttr);
                foreach (var re in res)
                {
                    yield return re;
                }

                if (item.HasAttributes && hasAttr.Invoke())
                {
                    foreach (var attr in item.Attributes)
                    {
                        if (IsContainKeyword(attr.Value, keyword))
                        {
                            yield return attr.XPath;
                        }
                    }
                }
            }

            if (node.NodeType == HtmlNodeType.Text && IsContainKeyword(node.InnerText, keyword)) //
            {
                yield return node.ParentNode.XPath;
            }
        }

        public static IEnumerable<CrawTarget> SearchPropertiesSmart(this string htmlDoc,
            ICollection<CrawlItem> existItems = null)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlDoc);
            return html.SearchPropertiesSmart(existItems);
        }

        /// <summary>
        ///     搜索XPath,同时在InnerText和Attribute中查找
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static IEnumerable<string> SearchXPath(this HtmlDocument doc, string keyword, Func<bool> hasAttr)
        {
            keyword = keyword.Trim();
            if (string.IsNullOrEmpty(keyword))
                yield break;

            var xpaths = doc.DocumentNode.SearchXPath(keyword, hasAttr);
            foreach (var xpath in xpaths)
            {
                yield return xpath;
            }
        }



        /// <summary>
        ///     查询XPATH，返回CrawlItem
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="keyword"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static CrawlItem SearchXPath(this HtmlDocument doc, string keyword, string name, bool hasAttr = true)
        {
            var xpath = doc.SearchXPath(keyword, () => hasAttr).FirstOrDefault();
            if (xpath == null)
                return null;
            var crawitem = new CrawlItem {XPath = xpath, SampleData1 = keyword, Name = name}
                ;
            return crawitem;
        }

        /// <summary>
        ///     搜索XPath,同时在InnerText和Attribute中查找
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static IEnumerable<string> SearchXPath(this string html, string keyword, Func<bool> isAttr)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            keyword = keyword.Trim();
            var paths = doc.DocumentNode.SearchXPath(keyword, isAttr);

            foreach (var item in paths)
            {
                yield return item;
            }
        }

        private static bool IsSameXPath(string xpath1, string xpath2, string shortv)
        {
            var p1 = XPath.TakeOff(xpath1, shortv); 
            var p2 = XPath.TakeOff(xpath2, shortv);
            return p1 == p2;
        }

        private static List<CrawlItem> GetDiffNodes(HtmlDocument doc2, string root, bool isAttrEnabled,
            IEnumerable<CrawlItem> exists = null,int minNodeCount=2)
        {
            HtmlNodeCollection nodes = null;
            List<CrawlItem> crawlItems=new List<CrawlItem>();
            try
            {
                nodes = doc2.DocumentNode.SelectNodes(root);
            }
            catch (Exception ex)
            {
                XLogSys.Print.Error(ex.Message + "  可能XPath表达式有误");
                return new List<CrawlItem>();
            }

            if (nodes == null|| nodes.Count<minNodeCount )
            {

                return new List<CrawlItem>();
            }

            var buffers = new List<List<string>>();
            var nodes3 = nodes.ToList(); // .Where(d => d.Name.Contains("#") == false).ToList();
            if (nodes3.Count > 1)
            {
                GetDiffNodes(nodes3, crawlItems, buffers, isAttrEnabled);
            }
            if (exists != null)
            {
                crawlItems.RemoveElementsNoReturn(d => exists.Any(r => IsSameXPath(d.XPath, r.XPath, root)));
                crawlItems.AddRange(exists);
            }

            return crawlItems;
        }

        private static CrawTarget getCrawTarget(List<CrawlItem> items,string root=null)
        {
            if (items.Count > 1)
                 return new CrawTarget(items);
            else if (items.Count == 1&&root!=null)
            {
                var child = XPath.TakeOff(items[0].XPath, root);
                items[0].XPath = child;
                return new CrawTarget(items, root);

            }
            return null;
        }
        public static IEnumerable<CrawTarget> SearchPropertiesSmart(this HtmlDocument doc2,
            ICollection<CrawlItem> existItems = null, string rootPath = null, bool isAttrEnabled = false)
        {
            if (existItems == null)
                existItems = new List<CrawlItem>();
            var shortv = "";
            var dict = new Dictionary<string, double>();
            if (string.IsNullOrEmpty(rootPath))
            {
                if (existItems.Count > 1)
                {
                    shortv =
                        XPath.GetMaxCompareXPath(existItems.Select(d => d.XPath));

                    var items = GetDiffNodes(doc2, shortv, isAttrEnabled, existItems,1);
                    var target = getCrawTarget(items, shortv);
                    if (target != null)
                        yield return target;
                    yield break;
                }

                if (existItems.Count == 1)
                {
                    var realPath = existItems.First().XPath;
                    var items = XPath.Split(realPath);
                    var array =
                       items.SelectAll(d => true)
                            .Select(d => XPath.SubXPath(realPath,d)).ToList();
                    foreach (var item in array)
                    {
                        GetTableRootProbability(
                            doc2.DocumentNode.SelectSingleNode(item)
                                .ChildNodes.Where(d2 => d2.Name.Contains("#") == false)
                                .ToList(), dict, false);
                    }
                }
                else
                {
                    GetTableRootProbability(
                        doc2.DocumentNode.ChildNodes.Where(d => d.Name.Contains("#") == false).ToList(), dict, true);
                }
                if (existItems.Count < 2)
                {
                    IEnumerable<KeyValuePair<string, double>> p = dict.OrderByDescending(d => d.Value);
                    foreach (var keyValuePair in p)
                    {
                        var items = GetDiffNodes(doc2, keyValuePair.Key, isAttrEnabled, existItems,4);
                        var target = getCrawTarget(items, keyValuePair.Key);
                        if (target != null)
                            yield return target;

                    }
                }
            }
            else
            {
                var items = GetDiffNodes(doc2, rootPath, isAttrEnabled, new List<CrawlItem>());
                if (items.Count>0)
                {
                    var root = doc2.DocumentNode.SelectSingleNode(rootPath);

                    foreach (var crawlItem in items)
                    {
                        crawlItem.XPath = XPath.TakeOff(crawlItem.XPath, root.XPath);
                    }

                    yield return new CrawTarget(items, rootPath);
                }
                    
            }
        }

        public static IEnumerable<List<FreeDocument>> GetDataFromMultiURL(IEnumerable<string> url)
        {
            CrawTarget properties = null;
            foreach (var r in url)
            {
                var doc2 = GetDocumentFromURL(r);
                if (properties == null)

                    properties = doc2.SearchPropertiesSmart().FirstOrDefault();
                if (properties?.CrawItems != null)
                {
                    yield return doc2.GetDataFromXPath(properties.CrawItems);
                }
            }
        }

        /// <summary>
        ///     直接从指定的URL获取数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<FreeDocument> GetDataFromURL(string url)
        {
            return GetMultiDataFromURL(url).FirstOrDefault();
        }

        public static HtmlDocument GetHtmlDocument(string url)
        {
            var httpitem = new HttpItem {URL = url};
            var helper = new HttpHelper();
            HttpStatusCode statusCode;
            var doc2 = helper.GetHtml(httpitem, out statusCode);
            if (statusCode != HttpStatusCode.OK)
                return null;
            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(doc2);
            return htmldoc;
        }

        public static IEnumerable<List<FreeDocument>> GetMultiDataFromURL(string url)
        {
            var httpitem = new HttpItem {URL = url};
            var helper = new HttpHelper();
            HttpStatusCode statusCode;
            var doc2 = helper.GetHtml(httpitem, out statusCode);
            if (statusCode != HttpStatusCode.OK)
                yield break;

            if (doc2 == null)
                yield return new List<FreeDocument>();
            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(doc2);

            foreach (var item in htmldoc.GetDataFromHtml())
            {
                yield return item;
            }
        }

        /// <summary>
        ///     直接从HTML中获取信息，全部自动化
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static IEnumerable<List<FreeDocument>> GetDataFromHtml(this HtmlDocument doc)
        {
            var properties = doc.SearchPropertiesSmart();
            return properties.Select(property => doc.GetDataFromXPath(property.CrawItems));
        }

        public static string CompileCrawItems(this HtmlDocument doc2, IList<CrawlItem> crawlItem)
        {
            var shortv =
                XPath.GetMaxCompareXPath(crawlItem.Select(d => d.XPath));
            if (!string.IsNullOrEmpty(shortv))
            {
                crawlItem.Execute(d => d.XPath = XPath.TakeOff(d.XPath, shortv)); 
                return shortv;
            }
            return "";
        }

        public static List<FreeDocument> GetDataFromXPath(this HtmlDocument doc2, IList<CrawlItem> crawlItems,
            ListType type = ListType.List, string rootXPath = "")
        {
            if (crawlItems.Count == 0)
                return new List<FreeDocument>();

            var documents = new List<FreeDocument>();

            switch (type)
            {
                case ListType.List:
                    var root = "";
                    var takeoff = "";
                    if (string.IsNullOrEmpty(rootXPath))
                    {
                        root =
                            XPath.GetMaxCompareXPath(crawlItems.Select(d => d.XPath));
                        takeoff = root;
                    }
                    else
                    {
                        root = rootXPath;
                    }


                    var nodes = doc2.DocumentNode.SelectNodes(root);

                    if (nodes == null)
                        break;
                    foreach (var node in nodes)
                    {
                        var document = new FreeDocument();
                        foreach (var r in crawlItems)
                        {
                            string path;
                            if (string.IsNullOrEmpty(takeoff))

                                path = node.XPath + r.XPath;
                            else
                            {
                                path = node.XPath + XPath.TakeOff(r.XPath, takeoff);

                            }

                            var result = node.GetDataFromXPath(path, r.IsHTML);


                            document.SetValue(r.Name, result);
                        }
                        documents.Add(document);
                    }
                    return documents;

                case ListType.One:


                    var freeDocument = new FreeDocument();


                    foreach (var r in crawlItems)
                    {
                        doc2.GetDataFromXPath(r, freeDocument);
                    }

                    return new List<FreeDocument> {freeDocument};
            }
            return new List<FreeDocument>();
        }

        public class CrawTarget
        {
            public CrawTarget(List<CrawlItem> items, string rootpath="")
            {
                CrawItems = items;
                RootXPath = rootpath;
            }
            public string RootXPath { get; set; }
            public List<CrawlItem> CrawItems { get; set; }
        }

        private class ParaClass
        {
            public int tLen { get; set; }
            public string Path { get; set; }
        }
    }
}