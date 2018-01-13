using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using Hawk.Core.Utils.Logs;

namespace Hawk.Core.Utils
{
    /// <summary>
    /// 系统序列化器
    /// </summary>
    public class CustomSerializer
    {
        // Public Methods (14) 

        #region Public Methods

        /// <summary>
        /// 二进制集合反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataobject"></param>
        /// <param name="theStream"></param>
        public static void BinaryStreamCollectionDeserialize<T>(ICollection<T> dataobject, Stream theStream)
        {
            IFormatter formter = new BinaryFormatter();
            while (theStream.Position < theStream.Length)
            {
                dataobject.Add((T)formter.Deserialize(theStream));
            }
        }

        /// <summary>
        /// 二进制反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="theStream"></param>
        /// <returns></returns>
        public static T BinaryStreamDeserialize<T>(Stream theStream)
        {
            IFormatter formter = new BinaryFormatter();
            T mm = default(T);
            try
            {
                mm = (T)formter.Deserialize(theStream);
            }
            catch (Exception ex)
            {
            }

            return mm;
        }

        public static void BinaryStreamSerialize<T>(T obj, StreamWriter theStreamWriter)
        {
            var mySerializer = new BinaryFormatter();

            if (theStreamWriter.BaseStream is NetworkStream) //网络流则加入字符包长
            {
                var mem1 = new MemoryStream();
                mySerializer.Serialize(mem1, obj);
                byte[] data = mem1.ToArray();
                var packetSize = (int)mem1.Length;

                mem1.Seek(0, SeekOrigin.Begin);
                var ob = BinaryStreamDeserialize<T>(mem1);

                mem1.Close();

                byte[] size = BitConverter.GetBytes(packetSize);
                var final = new byte[(data.Length + size.Length)];
                size.CopyTo(final, 0);
                data.CopyTo(final, 4);
                try
                {
                    theStreamWriter.BaseStream.BeginWrite(final, 0, final.Length, null, theStreamWriter.BaseStream);
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error(ex.Message);
                }
            }
            else
            {
                try
                {
                    mySerializer.Serialize(theStreamWriter.BaseStream, obj);
                }
                catch
                {
                }
            }
        }

        public static T Deserialize<T>(string filePosition)
        {
            T rc = default(T);

            var serializer = new XmlSerializer(typeof(T));

            // A FileStream is needed to read the XML document.   
            using ( var fs = new FileStream(filePosition, FileMode.Open, FileAccess.Read))
            {
                XmlReader reader = XmlReader.Create(fs);


                // Use the Deserialize method to restore the object's state.   
                rc = (T)serializer.Deserialize(reader);
              
                reader.Close();
                return rc;
            }
           
         
        }

        /// <summary>
        /// Deserializes the given object.
        /// </summary>
        /// <typeparam name="T">The type of the object to be deserialized.</typeparam>
        /// <param name="reader">The reader used to retrieve the serialized object.</param>
        /// <param name="extraTypes"><c>Type</c> array
        ///           of additional object types to deserialize.</param>
        /// <returns>The deserialized object of type T.</returns>
        public static T Deserialize<T>(XmlReader reader, Type[] extraTypes)
        {
            var xs = new XmlSerializer(typeof(T), extraTypes);
         
             var item= (T)xs.Deserialize(reader);
             reader.Close();
            return item;

        }

        public static List<T> Deserialize<T>(string filePosition, Type dataCollectionType)
        {
            try
            {
                var serializer = new XmlSerializer(dataCollectionType.MakeArrayType());

                // A FileStream is needed to read the XML document.   
                var fs = new FileStream(filePosition, FileMode.Open);
                XmlReader reader = XmlReader.Create(fs);
                var temp = (IList<T>)serializer.Deserialize(reader);
                fs.Close();
                return temp.ToList();

                // Use the Deserialize method to restore the object's state.   

            
            }
            catch
            {
            }
            return null;
        }

        public static void Deserialize<T>(string filePosition, Type dataCollectionType, IEnumerable<T> dataCollection)
        {
            if (dataCollection == null)
            {
                throw new ArgumentNullException("dataCollection");
            }

            try
            {
                var serializer = new XmlSerializer(dataCollectionType.MakeArrayType());

                // A FileStream is needed to read the XML document.   
                var fs = new FileStream(filePosition, FileMode.Open);
                XmlReader reader = XmlReader.Create(fs);
                var temp = (IList<T>)serializer.Deserialize(reader);

                dataCollection = temp.ToList();

                // Use the Deserialize method to restore the object's state.   

                fs.Close();
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">抽象接口</typeparam>
        /// <typeparam name="V">实例接口</typeparam>
        /// <param name="data"></param>
        /// <param name="fileList"></param>
        public static void FileListDataDeserialize<T, V>(IList<T> data, string[] fileList) where V : T
        {
            foreach (string file in fileList)
            {
                if (Path.GetExtension(file) == ".xml")
                {
                    foreach (T rc in Deserialize<List<V>>(file))
                    {
                        data.Add(rc);
                    }
                }
            }
        }

        public static void FileListDataDeserialize<T>(IEnumerable<T> data, string[] fileList, Type dataType)
            where T : class
        {
            foreach (string file in fileList)
            {
                if (Path.GetExtension(file) == ".xml")
                {
                    Deserialize(file, dataType, data);
                }
            }
        }

        public static void FolderDataDeserialize<T, V>(IList<T> data, string folderLocation) where V : T
        {
            string[] fileList = Directory.GetFileSystemEntries(folderLocation);

            FileListDataDeserialize<T, V>(data, fileList);
        }

        public static void FolderDataDeserialize<T>(IEnumerable<T> data, string folderLocation, Type dataType)
            where T : class
        {
            string[] fileList = Directory.GetFileSystemEntries(folderLocation);

            FileListDataDeserialize(data, fileList, dataType);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">要序列化的数据</param>
        /// <param name="xmlFilePosition">要保存的文件名称</param>
        /// <remarks>该函数内部捕捉了异常</remarks>
        public static void Serialize<T>(T obj, string xmlFilePosition)
        {
            var myWriter = new StreamWriter(xmlFilePosition);

            var mySerializer = new XmlSerializer(obj.GetType());
            try
            {
                mySerializer.Serialize(myWriter, obj);
            }
            catch
            {
            }

            myWriter.Close();
        }

        public static string Serialize<T>(T obj, Type[] extraTypes)
        {
            StringWriter sw = null;
            try
            {
                var xs = new XmlSerializer(typeof(T), extraTypes);
                sw = new StringWriter();
                xs.Serialize(sw, obj);
                sw.Flush();
                return sw.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                }
            }
        }

        public static void Serialize(object obj, string xmlFilePosition)
        {
            var myWriter = new StreamWriter(xmlFilePosition);

            var mySerializer = new XmlSerializer(obj.GetType());
            try
            {
                mySerializer.Serialize(myWriter, obj);
            }
            catch
            {
            }

            myWriter.Close();
        }

        public static void Serialize(object dataCollection, string xmlFilePosition, Type dataType)
        {
            var myWriter = new StreamWriter(xmlFilePosition);
            Type collectionType = typeof(List<>);
            Type rc = collectionType.MakeGenericType(dataType);

            var mySerializer = new XmlSerializer(rc);
            try
            {
                mySerializer.Serialize(myWriter, dataCollection);
            }
            catch
            {
            }

            myWriter.Close();
        }

        #endregion
    }
}