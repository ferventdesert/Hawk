using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace Hawk.Core.Connectors
{
    [XFrmWork("Json导入导出器",  "输出文本型JSON文件", "")]
    public class FileConnectorJson : FileConnector
    {
        #region Properties

        public override string ExtentFileName => ".json";

        #endregion

        #region Methods

        protected object FormatJsonData(string str)
        {

            if (str.Contains("ObjectId"))
            {
                str = str.Replace("ObjectId(", "");
                str = str.Replace("\"),", "\",");
            }

            object totals = JsonConvert.Import(str);
            return totals;
        }

        public IEnumerable<FreeDocument> ReadText(string text, Action<int> alreadyGetSize = null)
        {
            var totals = FormatJsonData(text);

            if (totals == null)
            {
                throw new Exception("文件不是合法的Json文件");
            }
            var array = totals as JsonArray;
            if (array != null)
            {
                alreadyGetSize?.Invoke(array.Count);
                foreach (object d in array)
                {
                    var data =  new FreeDocument();
                    ItemtoNode(d, data);
                    yield return data;
                }
            }
            var obj = totals as JsonObject;
            if (obj != null)
            {
                if (alreadyGetSize != null)
                {
                    alreadyGetSize(1);
                }
                var data =new FreeDocument(); 
                ItemtoNode(obj, data);
                yield return data;
            }
        }

        public override IEnumerable<FreeDocument> ReadFile(Action<int> alreadyGetSize = null)
        {
            string str = File.ReadAllText(FileName, Encoding.UTF8);
            return ReadText(str, alreadyGetSize);
        }

        private int ItemtoNode(object d, IFreeDocument dict)
        {


            var doc = dict as FreeDocument;
            if (doc == null)
            {
                if (d is JsonObject)
                    dict.DictDeserialize(d as JsonObject);
            }
            else
            {
                if (d is JsonArray)
                {
                    foreach (var item in d as JsonArray)
                    {
                        var item2 = new FreeDocument();
                        ItemtoNode(item, item2);
                        if (doc.Children == null)
                        {
                            doc.Children = new List<FreeDocument>();
                        }
                        doc.Children.Add(item2);

                    }
                    return 1;
                }
                else if (d is JsonObject)
                {
                    var jb = d as JsonObject;
                    foreach (var b in jb)
                    {
                        var dict2 = new FreeDocument();
                        var res = ItemtoNode(b.Value, dict2);
                        if (res == 0)
                        {
                            doc.Add(b.Name, b.Value);
                        }
                        else if (res == 1)
                        {
                            doc.Add(b.Name, dict2);
                        }
                    }

                    return 2;

                }
            }
            return 0;

        }

        private static  object Node2Item(object dic)
        {
            JsonObject js = new JsonObject();
            if (dic is IFreeDocument)
            {
                var res = (dic as IFreeDocument).DictSerialize();
                js.AddRange(res);
                var fre = dic as FreeDocument;
                if (fre != null)
                {
                    if (fre.Children != null)
                    {
                        JsonArray array = new JsonArray();
                        foreach (var child in fre.Children)
                        {
                            array.Add(Node2Item(child));
                        }
                        if (!js.HasMembers)
                            return array;
                        js.Add("Children", array);
                    }
                }
            }
            else
            {
                return dic;
            }

            return js;
        }

        public static JsonObject GetJsonObject(IFreeDocument data)
        {
            IEnumerable<KeyValuePair<string, object>> dicts =
                          data.DictSerialize();
            string[] keys = dicts.Select(d => d.Key).ToArray();
            object[] value = dicts.Select(d => Node2Item(d.Value)).ToArray();
            var rc = new JsonObject(keys, value);
            return rc;
        }
        public override string GetString(IEnumerable<IFreeDocument> datas)
        {
            var nodeGroup = new JsonArray();

            foreach (IFreeDocument data in datas)
            {

                nodeGroup.Add(GetJsonObject(data));
            }
            return nodeGroup.ToString();
        }


        public override IEnumerable<IFreeDocument> WriteData(IEnumerable<IFreeDocument> datas)
        {


            using (TextWriter streamWriter = new StreamWriter(FileName, false, Encoding.UTF8))
            {
                using (JsonWriter writer = CreateJsonWriter(streamWriter))
                {
                    int i = 0;

                    using (var dis = new DisposeHelper(() =>
                    {
                        writer.WriteEndArray();
                    }))
                    {
                        writer.WriteStartArray();
                        foreach (var data in datas)
                        {

                            writer.WriteString(GetJsonObject(data).ToString());
                           streamWriter.Write("\n");

                            yield return data;
                            i++;
                        }
                    }


                }
            }

        }


        private JsonWriter CreateJsonWriter(TextWriter @out)
        {
            var jsonWriter = new JsonTextWriter(@out) { PrettyPrint = true };
            return jsonWriter;
        }

        #endregion
    }
}