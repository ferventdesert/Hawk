using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    [XFrmWork("XML导入导出器",  "输出和输入XML文件", "")]
    public class FileConnectorXML : FileConnector
    {
        public override string ExtentFileName => ".xml";

        /// <summary>
        ///     将XmlDocument转化为string
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        public static  string ConvertXmlToString(XmlDocument xmlDoc)
        {
            var stream = new MemoryStream();
            var writer = new XmlTextWriter(stream, null);
            writer.Formatting = Formatting.Indented;
            xmlDoc.Save(writer);

            var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            string xmlString = sr.ReadToEnd();
            sr.Close();
            stream.Close();

            return xmlString;
        }

        private void XMLNode2Dict(XmlNode xnode, FreeDocument dict)
        {
            if (xnode.Attributes != null)
            {
                for (int i = 0; i < xnode.Attributes.Count; i++)
                {
                    dict.Add(xnode.Attributes[i].Name, xnode.Attributes[i].Value);
                }
            }

            if (xnode.HasChildNodes)
            {
                for (int i = 0; i < xnode.ChildNodes.Count; i++)
                {
                    var docu = new FreeDocument();

                    XmlNode n = xnode.ChildNodes[i];
                    if (n.Name == "Children")
                    {
                        if (dict.Children == null)
                        {
                            dict.Children = new List<FreeDocument>();
                        }

                        docu.Name = n.Name;
                        XMLNode2Dict(n, docu);
                        dict.Children.Add(docu);
                    }
                    else
                    {
                        docu.Name = n.Name;
                        XMLNode2Dict(n, docu);
                        dict.Add(docu.Name, docu);
                    }
                }
            }
        }

        private IEnumerable<FreeDocument> ReadText(XmlDocument xdoc, Action<int> alreadyGetSize = null)
        {
            XmlNode xTable = xdoc.DocumentElement;
            if (xTable == null)
                yield break;

            alreadyGetSize?.Invoke(xTable.ChildNodes.Count);
            foreach (XmlNode xnode in xTable)
            {

                var data = new FreeDocument(); 
                var dict = new FreeDocument();
                dict.Name = xnode.Name;
                XMLNode2Dict(xnode, dict);

                data.DictDeserialize(dict.DictSerialize());
                var doc = data as FreeDocument;
                if (doc != null)
                {
                    doc.Children = dict.Children;
                }
                yield return data;
            }
        }

        public IEnumerable<IFreeDocument> ReadText(string text, Action<int> alreadyGetSize = null)
        {
            var xdoc = new XmlDocument();
            xdoc.LoadXml(text);
            return ReadText(xdoc, alreadyGetSize);

        }
        public override IEnumerable<FreeDocument> ReadFile(Action<int> alreadyGetSize = null)
        {

            var xdoc = new XmlDocument();

            ControlExtended.SafeInvoke(() => xdoc.Load(FileName), Utils.Logs.LogType.Important);
            
           
            return ReadText(xdoc, alreadyGetSize);
        }

        public static  void Node2XML(IEnumerable<KeyValuePair<string, object>> data, XmlNode node, XmlDocument docu)
        {
            var doc = data as FreeDocument;
            if (doc != null)
            {
                foreach (var item in doc.DataItems.OrderBy(d=>d.Key))
                {
                    if (item.Value is IDictionary<string, object>)
                    {
                        var dict = item.Value as IDictionary<string, object>;
                        XmlNode newNode = docu.CreateNode(XmlNodeType.Element, item.Key, "");
                        Node2XML(dict, newNode, docu);
                        node.AppendChild(newNode);
                    }
                    else
                    {
                        XmlAttribute attr = docu.CreateAttribute(item.Key);
                        attr.InnerText = item.Value?.ToString() ?? "";
                        node.Attributes.Append(attr);
                    }
                }
                if (doc.Children == null)
                    return;
                foreach (FreeDocument child in doc.Children)
                {
                    child.Name = child.Name.Replace("#", "");
                    XmlNode newNode = docu.CreateNode(XmlNodeType.Element, "Children", "");
                    Node2XML(child, newNode, docu);
                    node.AppendChild(newNode);
                }
            }
            else
            {
                if (data != null)
                {
                    foreach (var o in data)
                    {
                        XmlAttribute attr = docu.CreateAttribute(o.Key);
                        attr.InnerText = o.Value?.ToString() ?? "";

                        node.Attributes.Append(attr);
                    }
                }
            }
        }

        public override IEnumerable<IFreeDocument> WriteData(IEnumerable<IFreeDocument> datas)
        {

            var doc = new XmlDocument(); // 创建dom对象
            XmlElement root = doc.CreateElement("root");
            using (var dis = new DisposeHelper(() =>
            {
                doc.AppendChild(root);
                doc.Save(FileName);
            }))
            {
                foreach (IFreeDocument dictionarySerializable in datas)
                {
                    var doc2 = dictionarySerializable.DictSerialize();

                    XmlElement newNode = doc.CreateElement(doc2 == null ? "Element" : doc2.Name);
                    Node2XML(doc2, newNode, doc);
                    root.AppendChild(newNode);
                    yield return dictionarySerializable;
                }
            }

            // 保存文件

        }

        public static string GetString(FreeDocument data)
        {
            var doc = new XmlDocument(); // 创建dom对象
            XmlElement root = doc.CreateElement("root");
            XmlElement newNode = doc.CreateElement(data == null ? "Element" : data.Name);
            Node2XML(data, newNode, doc);
            root.AppendChild(newNode);
            doc.AppendChild(root);
            return ConvertXmlToString(doc);
        }
        public override string GetString(IEnumerable<IFreeDocument> datas)
        {
            var doc = new XmlDocument(); // 创建dom对象
            XmlElement root = doc.CreateElement("root");
            if (datas != null)
            {
                foreach (IFreeDocument dictionarySerializable in datas)
                {
                    var doc2 = dictionarySerializable.DictSerialize();

                    XmlElement newNode = doc.CreateElement(doc2 == null ? "Element" : doc2.Name);
                    Node2XML(doc2, newNode, doc);

                    root.AppendChild(newNode);
                }
            }

            doc.AppendChild(root);
            return ConvertXmlToString(doc);
        }
    }
}