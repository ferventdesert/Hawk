using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Hawk.Core.Utils
{
    public class SerializableDictionary<TKey, TValue>
             : Dictionary<TKey, TValue>, IXmlSerializable
    {
        public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> item)
        {
            foreach (var o in item)
            {
                this.Add(o.Key,o.Value);
            }
        }

        public SerializableDictionary( )
        { 
        }
        #region IXmlSerializable 成员

        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader"></param>
        public void ReadXml(XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
            if (reader.IsEmptyElement || !reader.Read())
            {
                return;
            }
           
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.ReadStartElement("item");

                    reader.ReadStartElement("key");
                    TKey key = (TKey)keySerializer.Deserialize(reader);
                    reader.ReadEndElement();
                    reader.ReadStartElement("value");
                    TValue value = (TValue)valueSerializer.Deserialize(reader);
                    reader.ReadEndElement();

                    reader.ReadEndElement();
                    reader.MoveToContent();
                    this.Add(key, value);
                }
                reader.ReadEndElement();
           
        
         
           
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="writer"></param>
        public void WriteXml(XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                valueSerializer.Serialize(writer, this[key]);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        #endregion
    }
}
