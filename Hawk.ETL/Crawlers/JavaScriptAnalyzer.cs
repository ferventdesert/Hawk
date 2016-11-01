using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using Hawk.Core.Utils.Logs;
using HtmlAgilityPack;
using Jint.Native;
using Jint.Parser;
using Jint.Parser.Ast;
using Newtonsoft.Json;

namespace Hawk.ETL.Crawlers
{
    public class JavaScriptAnalyzer
    {
        static JavaScriptAnalyzer()
        {
        }
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

        public static Dictionary<string, object> HtmlSerialize(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return HtmlObjectSerialize(doc.DocumentNode);
        }
        public static Dictionary<string, object> HtmlObjectSerialize(HtmlNode doc)
        {
            if (doc.Name.ToLower() == "script")
            {

                return JsSeriaize(doc.InnerText);
            }
            var result = new Dictionary<string, object>();
            foreach (var attr in doc.Attributes)
            {
                result[attr.Name] = attr.Value;
            }
            if (doc.ChildNodes == null || doc.ChildNodes.Count == 0)
                return result;
            var childs = new ArrayList();
            foreach (var child in doc.ChildNodes)
            {

                var res = HtmlObjectSerialize(child);
                if (res.Any())
                    childs.Add(res);
            }
            if (childs.Count > 0)
                result["chi"] = childs;
            return result;
        }
        private static bool isBasicType(Object obj)
        {
            if (obj is int || obj is string || obj is double || obj is bool || obj is Enum || obj == null)
            {
                return true;
            }
            return false;
        }
        protected static Regex regex = new Regex(@"<\w+>");


        public static Dictionary<string, object> JsSeriaize(string js)
        {
            var parser = new JavaScriptParser();
            var program = parser.Parse(js);
            return JsObjectSerialize(program) as Dictionary<string, object>;
        }
        public static object JsObjectSerialize(object obj)
        {

            if (obj is string)
            {
                var str = obj as string;
                if (isHtml(str))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(str);
                    var res = HtmlObjectSerialize(doc.DocumentNode);
                    return res;
                }
            }
            if (isBasicType(obj))
                return obj;
            if (obj is JsValue)
            {
                obj = obj.ToString();

                return obj;
            }
            if (obj is Literal)
            {
                return JsObjectSerialize((obj as Literal).Value);
            }
            var result = new Dictionary<string, object>();
            if (obj is Property)
            {
                var p = obj as Property;
                result[p.Key.GetKey()] = JsObjectSerialize(p.Value);
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
                            var res = JsObjectSerialize(i);
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
                        value = JsObjectSerialize(value);
                    }

                    result[item2.Name] = value;
                }
            }
            return result;
        }

        public static bool isHtml(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;
            code = code.Substring(0, Math.Min(200, code.Length));
            if (regex.IsMatch(code))
                return true;
            return false;
        }
        public static object Parse(string code)
        {
            code = code.Trim();
            if (code.StartsWith("{") || code.StartsWith("["))
            {
                return serialier.DeserializeObject(code);

            }
            else if (isHtml(code))
            {
                return HtmlSerialize(code);
            }


            else
            {
                return JsSeriaize(code);
            }
        }
        static JavaScriptSerializer serialier = new JavaScriptSerializer();
        public static string Parse2XML(string code)
        {
            var root = Parse(code);

            var json = serialier.Serialize(root);
            var  doc = JsonConvert.DeserializeXNode(json,"Root",false);
            return doc.ToString();
        }

        static Regex reUnicode = new Regex(@"\\u([0-9a-fA-F]{4})", RegexOptions.Compiled);
        public static string Decode(string s)
        {
            return reUnicode.Replace(s, m =>
            {
                short c;
                if (short.TryParse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out c))
                {
                    return "" + (char)c;
                }
                return m.Value;
            });
        }

        public static string Json2XML(string content, out bool isRealJson, bool isJson = false)
        {
            if (isJson)
            {
                try
                {


                    XmlDocument doc = JsonConvert.DeserializeXmlNode(content);
                    isRealJson = true;
                    return doc.OuterXml;
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
