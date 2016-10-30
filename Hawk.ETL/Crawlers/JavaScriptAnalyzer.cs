using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using Hawk.Core.Utils.Logs;
using Jint.Native;
using Jint.Parser.Ast;

namespace Hawk.ETL.Crawlers
{
    public class JavaScriptAnalyzer
    {
        private static readonly List<string> ignores = new List<string>
        {
            "Range",
            "Location",
            "Type",
            "Strict",
            "Operator",
            "Cached",
            "CachedValue",
            "CanBeCached"
        };

        private static bool isBasicType(Object obj)
        {
            if (obj is int || obj is string || obj is double || obj is bool || obj is Enum || obj == null)
            {
                return true;
            }
            return false;
        }

        public static object Serialize(object obj)
        {

            if (isBasicType(obj))
                return obj;
            if (obj is JsValue)
            {
                obj = obj.ToString();

                return obj;
            }
            if (obj is Literal)
            {
                return Serialize((obj as Literal).Value);
            }
            var result = new Dictionary<string, object>();
            if (obj is Property)
            {
                var p = obj as Property;
                result[p.Key.GetKey()] = Serialize(p.Value);
                return result;
            }
            var type = obj.GetType();
            var props = type.GetMembers();
            foreach (var item2 in props)

            {
                if (item2.MemberType == MemberTypes.Field || item2.MemberType == MemberTypes.Property)
                {
                    if (ignores.Contains(item2.Name))
                        continue;
                    object value = null;
                    if (item2 is FieldInfo)
                    {
                        value = (item2 as FieldInfo).GetValue(obj);
                    }
                    else if (item2 is PropertyInfo)
                    {
                        value = (item2 as PropertyInfo).GetValue(obj);
                    }

                    if (value == null)
                        continue;
                    if (value is IList)
                    {
                        var array = new ArrayList();
                        dynamic value2 = value;
                        foreach (var i in value2)
                        {
                            var res = Serialize(i);
                            if (i is Property)
                            {
                                var dict2 = res as IDictionary<string, object>;

                                foreach (var o in dict2)
                                {
                                    result[o.Key] = o.Value;
                                }
                            }
                            else
                            {
                                array.Add(res);
                            }

                        }
                        value = array;
                    }


                    else
                    {
                        value = Serialize(value);
                    }

                    result[item2.Name] = value;
                }
            }
            return result;
        }
        public static string Json2XML(string content, out bool isRealJson, bool isJson = false)
        {
            if (isJson)
            {
                try
                {
                    var serialier = new JavaScriptSerializer();
                    var result = serialier.DeserializeObject(content);

                    content = serialier.Serialize(result);

                    var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(content),
                        XmlDictionaryReaderQuotas.Max);
                    var doc = new XmlDocument();
                    doc.Load(reader);
                    isRealJson = true;
                    return doc.InnerXml;
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Debug("尝试转换为json出错：  " + ex.Message);
                }
            }
            isRealJson = false;
            return content;
        }
    }

}
