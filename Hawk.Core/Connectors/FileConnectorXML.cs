using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading;
using static System.Boolean;

namespace Hawk.Core.Connectors
{
    [XFrmWork("FileConnectorXML", "FileConnectorXML_desc", "")]
    public class FileConnectorXML : FileConnector
    {
        public bool IsZip = false;
        public override string ExtentFileName => ".xml .hproj";

        /// <summary>
        ///     将XmlDocument转化为string
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        public static string ConvertXmlToString(XmlDocument xmlDoc)
        {
            var stream = new MemoryStream();
            var writer = new XmlTextWriter(stream, null);
            writer.Formatting = Formatting.Indented;
            xmlDoc.Save(writer);

            var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var xmlString = sr.ReadToEnd();
            sr.Close();
            stream.Close();

            return xmlString;
        }
        public static List<FreeDocument> GetCollection(string datas)
        {
            FileConnectorXML connector = null;
            connector = new FileConnectorXML();
            return connector.ReadText(datas).ToList();
            ;
        }
        private void XMLNode2Dict(XmlNode xnode, FreeDocument dict)
        {
            if (xnode.Attributes != null)
            {
                for (var i = 0; i < xnode.Attributes.Count; i++)
                {
                    dict.Add(xnode.Attributes[i].Name, xnode.Attributes[i].Value);
                }
            }
            object _keeporder = false;
            bool keeporder = false;
            if (dict.TryGetValue(FreeDocument.KeepOrder, out _keeporder))
            {
                keeporder= Parse(_keeporder.ToString());
            }
            if (xnode.ChildNodes.Count < 200)
                keeporder = true;
            if (xnode.HasChildNodes)
            {
                if (!keeporder)
                {
                    Parallel.For(0, xnode.ChildNodes.Count, i =>
                    {
                        var docu = new FreeDocument();
                        var n = xnode.ChildNodes[i];

                        if (n.Name == "Children")
                        {
                            if (dict.Children == null)
                            {
                                dict.Children = new List<FreeDocument>();
                            }

                            docu.Name = n.Name;
                            XMLNode2Dict(n, docu);
                            Monitor.Enter(dict);
                            dict.Children.Add(docu);
                            Monitor.Exit(dict);
                        }
                        else
                        {
                            docu.Name = n.Name;
                            XMLNode2Dict(n, docu);
                            Monitor.Enter(dict);
                            dict.Add(docu.Name, docu);
                            Monitor.Exit(dict);
                        }
                    });
                }
                else
                {
                    for(var i=0;i< xnode.ChildNodes.Count;i++)
                    {
                        var docu = new FreeDocument();
                        var n = xnode.ChildNodes[i];

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
                var doc = data;
                doc.Children = dict.Children;
                yield return data;
            }
        }

        public IEnumerable<FreeDocument> ReadText(string text, Action<int> alreadyGetSize = null)
        {
            var xdoc = new XmlDocument();
            xdoc.LoadXml(text);
            return ReadText(xdoc, alreadyGetSize);
        }


        public  IEnumerable<FreeDocument> ReadFile(Stream stream,bool iszip, Action<int> alreadyGetSize = null)
        {
            var xdoc=new XmlDocument();
            if (iszip)
            {
                var zipstream = new ZipInputStream(stream);
                ZipEntry zipEntry = null;
                while ((zipEntry = zipstream.GetNextEntry()) != null)
                {
                    var byteArrayOutputStream = new MemoryStream();
                    zipstream.CopyTo(byteArrayOutputStream);
                    byte[] b = byteArrayOutputStream.ToArray();
                    string xml = System.Text.Encoding.UTF8.GetString(b, 0, b.Length);
                    ControlExtended.SafeInvoke(() => xdoc.LoadXml(xml), LogType.Important);
                    break;
                }
            }
            else
            {
                ControlExtended.SafeInvoke(() => xdoc.Load(stream), LogType.Important);
            }
            return ReadText(xdoc, alreadyGetSize);
        }



        public override IEnumerable<FreeDocument> ReadFile(Action<int> alreadyGetSize = null)
        {
          
            Stream stream = new FileStream(FileName, FileMode.Open);
            return ReadFile(stream, IsZip, alreadyGetSize);
        }

        public static void Node2XML(IEnumerable<KeyValuePair<string, object>> data, XmlNode node, XmlDocument docu)
        {
            var doc = data as FreeDocument;
            if (doc != null)
            {
                foreach (var item in doc.DataItems.OrderBy(d => d.Key))
                {
                    if (item.Value is IDictionary<string, object>)
                    {
                        var dict = item.Value as IDictionary<string, object>;
                        var newNode = docu.CreateNode(XmlNodeType.Element, item.Key, "");
                        Node2XML(dict, newNode, docu);
                        node.AppendChild(newNode);
                    }
                    else
                    {
                        var attr = docu.CreateAttribute(item.Key);
                        attr.InnerText = item.Value?.ToString() ?? "";
                        node.Attributes.Append(attr);
                    }
                }
                if (doc.Children == null)
                    return;
                foreach (var child in doc.Children)
                {
                    if (child == null)
                        continue;
                    child.Name = child.Name.Replace("#", "");
                    var newNode = docu.CreateNode(XmlNodeType.Element, "Children", "");
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
                        var attr = docu.CreateAttribute(o.Key);
                        attr.InnerText = o.Value?.ToString() ?? "";

                        node.Attributes.Append(attr);
                    }
                }
            }
        }

        public override IEnumerable<IFreeDocument> WriteData(IEnumerable<IFreeDocument> datas)
        {
            var doc = new XmlDocument(); // 创建dom对象
            var root = doc.CreateElement("root");
            using (var dis = new DisposeHelper(() =>
            {
                doc.AppendChild(root);
                Stream stream = new FileStream(FileName, FileMode.Create);
                if (IsZip)
                {
                    var zipStream = new ZipOutputStream(stream);
                    var ZipEntry = new ZipEntry(Path.GetFileName(FileName));
                    zipStream.PutNextEntry(ZipEntry);
                    zipStream.SetLevel(6);
                    stream = zipStream;
                    }
                doc.Save(stream);
                //stream.Flush();
                stream.Close();
            }))
            {
                foreach (var dictionarySerializable in datas)
                {
                    var doc2 = dictionarySerializable.DictSerialize();

                    var newNode = doc.CreateElement(doc2 == null ? "Element" : doc2.Name);
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
            var root = doc.CreateElement("root");
            var newNode = doc.CreateElement(data == null ? "Element" : data.Name);
            Node2XML(data, newNode, doc);
            root.AppendChild(newNode);
            doc.AppendChild(root);
            return ConvertXmlToString(doc);
        }

        public override string GetString(IEnumerable<IFreeDocument> datas)
        {
            var doc = new XmlDocument(); // 创建dom对象
            var root = doc.CreateElement("root");
            if (datas != null)
            {
                foreach (var dictionarySerializable in datas)
                {
                    var doc2 = dictionarySerializable.DictSerialize();

                    var newNode = doc.CreateElement(doc2 == null ? "Element" : doc2.Name);
                    Node2XML(doc2, newNode, doc);

                    root.AppendChild(newNode);
                }
            }

            doc.AppendChild(root);
            return ConvertXmlToString(doc);
        }
    }
}