using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using MongoDB;
using NPOI.OpenXmlFormats.Wordprocessing;

namespace Hawk.Core.Utils
{
    /// <summary>
    ///     集合逻辑类型
    /// </summary>
    public enum LogicType
    {
        AllRight,
        AnyWrong,
        AllWrong,
        AnyRight
    }

    /// <summary>
    ///     自定义的扩展方法
    /// </summary>
    public static class ExtendEnumerable
    {
        private static readonly Random random = new Random(unchecked((int) DateTime.Now.Ticks));

        public static string GenerateRandomString(int length)
        {
            var checkCode = string.Empty;

            var random = new Random();

            for (var i = 0; i < length; i++)
            {
                var number = random.Next();

                char code;
                if (number%2 == 0)
                {
                    code = (char) ('0' + (char) (number%10));
                }
                else
                {
                    code = (char) ('A' + (char) (number%26));
                }

                checkCode += code.ToString();
            }

            return checkCode;
        }

        public static void KeepRange<T>(this IList<T> collection, int start, int end)
        {
            if (start < 0)
                start = 0;
            if (end >= collection.Count)
                end = collection.Count;
            
            for (int i = collection.Count-1; i >end ; i--)
            {
                var item = collection[i];
                collection.Remove(item);

            }
            for (int i = 0; i < start; i++)
            {
                var item = collection[i];
                collection.Remove(item);
            }
        }
        public static IEnumerable<T> Init<T>(this IEnumerable<T> items, Func<T, bool> init = null)
        {
            var count = 0;
            foreach (var item in items)
            {
                if (count == 0)
                    if (init != null && init(item) == false)
                    {

                        yield break;
                    }
                count++;

                yield return item;
            }
        }

        public static IFreeDocument MergeQuery(this IFreeDocument document, IFreeDocument doc2, string columnNames)
        {
            if (doc2 == null || string.IsNullOrWhiteSpace(columnNames))
                return document;

            var columns = columnNames.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            if (columnNames.ToString() == "*")
                columns = doc2.Keys.ToArray();
            foreach (var column in columns)
            {
                document.SetValue(column, doc2[column]);
            }
            return document;
        }

        public static string Query(this IFreeDocument document, string query)
        {
            if (query == null)
                return null;
            query = query.Trim();
            if (query.StartsWith("[") && query.EndsWith("]"))
            {
                var len = query.Length;
                query = query.Substring(1, len - 2);
                var result = document?[query];
                return result?.ToString();
            }
            return query;
        }

        public static IEnumerable<IFreeDocument> Cross(this IEnumerable<IFreeDocument> datas,
            IEnumerable<IFreeDocument> target)
        {
            foreach (var data in datas)
            {
                foreach (var item in target)
                {
                    var data2 = data.Clone();
                    item.DictCopyTo(data2);
                    yield return data2;
                }
            }
        }

        public static IEnumerable<IFreeDocument> Cross(this IEnumerable<IFreeDocument> datas,
            Func<IFreeDocument, IEnumerable<IFreeDocument>> generator)
        {
            bool any = false;
            foreach (var data in datas)
            {
                foreach (var item in generator(data))
                {
                    var data2 = data.Clone();
                    item.DictCopyTo(data2);
                    yield return data2;
                }
                any = true;
            }
            if (any == false)
            {
                foreach (var item in generator(null))
                {
                  
                    yield return item;
                }
            }
        }

        public static Document ToMongoDocument(this IDictionary<string, object> item)
        {
            var doc = new Document(item);
            return doc;
        }

        public static List<int> GetRandomInts(int min, int max, int mount)
        {
            var values = new List<int>(mount);
            var index = 0;
            while (index < mount)
            {
                var value = random.Next(min, max);
                if (!values.Contains(value))
                {
                    values.Add(value);
                    index++;
                }
            }
            return values;
        }

        public static int GetRandonInt(int min, int max)
        {
            return random.Next(min, max);
        }

        #region Constants and Fields

        private static Type lastType;

        private static PropertyInfo[] propertys;

        #endregion

        #region Public Methods

        public static void OrThrows(this bool condition, string message = null)
        {
            if (!condition)
            {
                throw new Exception(message ?? "Something error");
            }
        }

        public static T Clone<T>(this T source) where T : class, IDictionarySerializable
        {
            if (source == null) return default(T);
            var type = source.GetType();
            var newItem = PluginProvider.GetObjectInstance(type) as IDictionarySerializable;
            source.DictCopyTo(newItem);
            return newItem as T;
        }

        public static bool LogicCheck(this IEnumerable<bool> checks, LogicType type)
        {
            switch (type)
            {
                case LogicType.AllRight:
                    return checks.All(d => d);
                case LogicType.AllWrong:
                    return checks.All(d => d == false);
                case LogicType.AnyRight:
                    return checks.Any(d => d);
                case LogicType.AnyWrong:
                    return checks.Any(d => d == false);
            }
            return false;
        }

        public static void AddRange<K, V>(this IDictionary<K, V> source, IDictionary<K, V> value)
        {
            if (value == null)
                return;
            foreach (var d in value)
            {
                source.SetValue(d.Key, d.Value);
            }
        }


        public static void AddRange<T>(this IList<T> source, IEnumerable<T> items)
        {
            foreach (var d in items)
            {
                source.Add(d);
            }
        }

        public static int MaxSameCount<T>(this IEnumerable<T> source, Func<T, T, bool> issame, out T maxvalue)
        {
            var dict = new Dictionary<T, int>();
            foreach (var t in source)
            {
                var add = false;
                foreach (var i in dict)
                {
                    if (issame(i.Key, t))
                    {
                        dict[i.Key]++;
                        add = true;
                        break;
                    }
                }
                if (add == false)
                    dict.Add(t, 1);
            }
            maxvalue = default(T);
            if (dict.Any() == false)
            {
                return 0;
            }
            var maxcount = 0;

            foreach (var i in dict)
            {
                if (i.Value > maxcount)
                {
                    maxcount = i.Value;
                    maxvalue = i.Key;
                }
            }

            return maxcount;
        }

        public static int MaxSameCount<T>(this IEnumerable<T> source, out T maxvalue)
        {
            var dict = new Dictionary<T, int>();
            foreach (var t in source)
            {
                if (dict.ContainsKey(t))
                    dict[t]++;
                else
                {
                    dict[t] = 0;
                }
            }
            maxvalue = default(T);
            if (dict.Any() == false)
            {
                return 0;
            }
            var maxcount = 0;

            foreach (var i in dict)
            {
                if (i.Value > maxcount)
                {
                    maxcount = i.Value;
                    maxvalue = i.Key;
                }
            }

            return maxcount;
        }


        public static int MaxSameCount<T>(this IEnumerable<T> source)
        {
            var dict = new Dictionary<T, int>();
            foreach (var t in source)
            {
                if (dict.ContainsKey(t))
                    dict[t]++;
                else
                {
                    dict[t] = 0;
                }
            }
            if (dict.Any() == false)
                return 0;
            return dict.Max(d => d.Value);
        }

        public static IDictionary<string, object> Clone(this IDictionary<string, object> old)
        {
            var dict = new Dictionary<string, object>();
            foreach (var o in old)
            {
                dict.Add(o.Key, o.Value);
            }
            return dict;
        }


        public static FreeDocument GetDiff(this IDictionary<string, object> d1,
            IDictionary<string, object> d2)
        {
            var doc = new FreeDocument();
            foreach (var o in d2)
            {
                if (d1.ContainsKey(o.Key) && d1[o.Key] == o.Value)
                    continue;
                doc.Add(o.Key, o.Value);
            }
            return doc;
        }

        public static IEnumerable<int> SelectAll<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> selector)
        {
            var item = source.ToList();
            for (var i = 0; i < item.Count; i++)
            {
                var p = item[i];
                if (selector(p))
                {
                    yield return i;
                }
            }
        }


        /// <summary>
        ///     获取序列中的最大元素值，并将其所在编号返回
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <param name="maxindex"></param>
        /// <returns></returns>
        public static int Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector,
            out int maxindex)
        {
            var t = 0;
            var index = 0;
            double max = 0;
            foreach (var item in source)
            {
                var i = selector(item);
                if (max < i)
                {
                    max = i;
                    index = t;
                }
                t++;
            }
            maxindex = index;
            return maxindex;
        }

        public static int MaxSameLength<T>(this IEnumerable<T> enumerable, T sameItem, out int start)
        {
            if (enumerable == null)
            {
                start = 0;

                return 0;
            }


            var dictionary = new Dictionary<int, int>();


            var count = 0;
            var len = 0;
            foreach (var item in enumerable)
            {
                if (!Equals(sameItem, item))
                {
                    if (count != 0)
                    {
                        dictionary.Add(count - len, len);
                    }


                    len = 0;
                }
                else
                {
                    len++;
                }
                count++;
            }


            var max = 0;
            var maxindex = 0;
            foreach (var i in dictionary)
            {
                if (i.Value > max)
                {
                    max = i.Value;
                    maxindex = i.Key;
                }
            }
            start = maxindex;

            return max;
        }

        public static T Search<T>(this IDictionary<string, T> dict, string name)
        {
            foreach (var item in dict)
            {
                if (item.Key.Contains(name)) return item.Value;
            }
            return default(T);
        }

        public static bool Contains(this IDictionarySerializable value, string content)
        {
            IDictionary<string, object> dataItems = value.DictSerialize(Scenario.Search);
            return
                dataItems.Where(d => d.Value != null).Select(item => item.Value.ToString()).Where(info => info != null).
                    Any(info => info.Contains(content));
        }

        public static void DictCopyTo(
            this IDictionarySerializable source, IDictionarySerializable dest, Scenario scenario = Scenario.Database)
        {
            dest.DictDeserialize(source.DictSerialize(scenario), scenario);
        }

        public static List<IDictionary<string, object>> DictTranslate(object value)
        {
            var result = new List<IDictionary<string, object>>();
            var d1 = value as List<Document>;
            if (d1 != null)
            {
                result.AddRange(d1.Cast<IDictionary<string, object>>());

                return result;
            }
            var d2 = value as JsonArray;
            if (d2 != null)
            {
                result.AddRange(d2.Cast<IDictionary<string, object>>());

                return result;
            }
            return result;
        }

        /// <summary>
        ///     对序列中的每个元素执行方法
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="method"></param>
        public static void Execute<TSource>(this IEnumerable<TSource> source, Action<TSource> method)
        {
            foreach (var d in source)
            {
                method(d);
            }
        }


        public static void ExecuteCommand(this ICollection<ICommand> commands, string name, params object[] paras)
        {
            var first = commands.OfType<Command>().FirstOrDefault(d => d.Text == name);
            first?.Execute(paras);
        }

        /// <summary>
        ///     对序列中的每个元素执行方法
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="method"></param>
        public static IEnumerable<TSource> ExecuteReturn<TSource>(
            this IEnumerable<TSource> source, Action<TSource> method)
        {
            foreach (var d in source)
            {
                method(d);
                yield return d;
            }
        }


        public static IEnumerable<KeyValuePair<string, object>> Filter(
            this IDictionary<string, object> dict, IEnumerable<string> names)
        {
            return names.Select(name => new KeyValuePair<string, object>(name, dict[name]));
        }

        public static T Get<T>(this IDictionary<string, object> dat, string key)
        {
            if (!dat.ContainsKey(key))
            {
                return default(T);
            }

            var item = default(T);
            var data = dat[key];

            var type = typeof (T);
            try
            {
                if (type.IsEnum)
                {
                    var s = data.ToString();

                    var res = (T) Enum.Parse(typeof (T), s);
                    return (res);
                }
                item = (T) Convert.ChangeType(dat[key], typeof (T));
            }
            catch (Exception ex)
            {
            }

            return item;
        }

        public static List<string> GetKeys(this IDictionarySerializable dat)
        {
            return dat.DictSerialize().Keys.ToList();
        }

        public static IEnumerable<string> GetKeys(this IEnumerable<IDictionarySerializable> source,
            Func<object, bool> filter = null, int count = 30)
        {
            if (source == null)
            {
                yield break;
            }
            if (!source.Any())
            {
                yield break;
            }
            var strList = new List<string>();
            var sample = source.Where(d => d != null).Take(count).Select(d => d.DictSerialize()).ToList();
            foreach (var item in sample)
            {
                foreach (var r in item.Keys)
                {
                    if (!strList.Contains(r))
                    {
                        strList.Add(r);
                    }
                }
            }
            foreach (var item in strList)
            {
                if (filter == null)
                    yield return item;
                else if (sample.Where(d => d[item] != null).Any(d => filter(d[item])))
                    yield return item;
            }
        }

        public static IEnumerable<T> MergeAll<T>(this IEnumerable<T> dict1, IEnumerable<T> dict2)
            where T : IFreeDocument
        {
            var f = dict1.GetEnumerator();
            var s = dict2.GetEnumerator();
            while (f.MoveNext())
            {
                s.MoveNext();
                f.Current.AddRange(s.Current);
                yield return f.Current;
            }
        }

        public static IEnumerable<T> Mix<T>(this IEnumerable<T> dict1, IEnumerable<T> dict2)
            where T : IFreeDocument
        {
            var f = dict1.GetEnumerator();
            var s = dict2.GetEnumerator();
            var i = 0;
            var fen = true;
            var sen = true;
            while (true)
            {
                if (i%2 == 0)
                {
                    if (fen && f.MoveNext())
                    {
                        yield return f.Current;
                    }
                    else
                    {
                        fen = false;
                    }
                    i++;
                }
                else if (i%2 == 1)
                {
                    if (sen && s.MoveNext())
                    {
                        yield return s.Current;
                    }
                    else
                    {
                        sen = false;
                    }
                    i++;
                }

                if (sen || fen == false)
                {
                    yield break;
                }
            }
        }

        // Extends String.Join for a smooth API.
        public static string Join(this string separator, IEnumerable<object> values)
        {
            return string.Join(separator, values);
        }


        /// <summary>
        ///     Get all digital type Column names;
        /// </summary>
        /// <param name="dat"></param>
        /// <returns></returns>
        public static List<string> GetNumbricKeys(this IDictionarySerializable dat)
        {
            var items = dat.DictSerialize();
            var allColumns =
                items.Where(d => AttributeHelper.IsNumeric(d.Value) || AttributeHelper.IsFloat(d.Value)).Select(
                    d => d.Key).ToList();
            return allColumns;
        }

        public static T GetRandom<T>(this IEnumerable<T> enumerable)
        {
            var enumerable1 = enumerable as T[] ?? enumerable.ToArray();
            var l = enumerable1.Count();
            if (l == 0)
            {
                return default(T);
            }
            return enumerable1.ElementAt(GetRandonInt(0, l - 1));
        }

        public static IEnumerable GetRange(this IEnumerable enumerable, int start, int end)
        {
            var e = enumerable.GetEnumerator();
            while (start > 0)
            {
                e.MoveNext();
            }
            while (end > 0)
            {
                yield return e.Current;
                e.MoveNext();
            }
        }

        public static void IncreaseSet<TK>(this IDictionary<TK, int> dat, TK k)
        {
            if (dat == null)
            {
                return;
            }
            if (dat.ContainsKey(k))
            {
                dat[k]++;
            }
            else
            {
                dat.Add(k, 1);
            }
        }

        public static bool IsEqual(this FreeDocument value, FreeDocument content)
        {
            var ignore= new List<string>() { "Description", "ScriptPath" };
            value.RemoveElements(d=>ignore.Contains(d));
          return  FileConnectorXML.GetString(value) == FileConnectorXML.GetString(content);
        }


        public static bool IsEqual(this IDictionarySerializable value, IDictionarySerializable content)
        {
            IDictionary<string, object> dic1 = value.DictSerialize();
            IDictionary<string, object> dic2 = content.DictSerialize();
            foreach (var o in dic1)
            {
                object res;
                if (dic2.TryGetValue(o.Key, out res))
                {
                    if (res != o.Value)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsEqual(this IDictionarySerializable value, IDictionary<string, object> content)
        {
            IDictionary<string, object> dic1 = value.DictSerialize();

            foreach (var o in dic1)
            {
                object res;
                if (content.TryGetValue(o.Key, out res))
                {
                    if (res != o.Value)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     对集合实现删除操作
        /// </summary>
        /// <typeparam name="TSource">元素类型</typeparam>
        /// <param name="source">要删除的元素列表</param>
        /// <param name="filter">过滤器</param>
        /// <param name="method">删除时执行的委托</param>
        public static IEnumerable<TSource> RemoveElements<TSource>(
            this IList<TSource> source, Func<TSource, bool> filter, Action<TSource> method = null)
        {
            var indexs = (from d in source where filter(d) select source.IndexOf(d)).ToList();
            indexs.Sort();
            for (var i = indexs.Count - 1; i >= 0; i--)
            {
                if (method != null)
                {
                    method(source[indexs[i]]);
                }
                yield return source[indexs[i]];
                source.RemoveAt(indexs[i]);
            }
        }

        public static void RemoveElements<K, V>(
            this IDictionary<K, V> source, Func<K, bool> filter, Action<V> method = null)
        {
            var indexs = (from d in source where filter(d.Key) select d.Key).ToList();

            for (var i = indexs.Count - 1; i >= 0; i--)
            {
                if (method != null)
                {
                    method(source[indexs[i]]);
                }

                source.Remove(indexs[i]);
            }
        }

        public static void RemoveAll<TSource>(
            this IList<TSource> source)

        {
            source.RemoveElementsNoReturn(d => true);
        }

        /// <summary>
        ///     对集合实现删除操作
        /// </summary>
        /// <typeparam name="TSource">元素类型</typeparam>
        /// <param name="source">要删除的元素列表</param>
        /// <param name="filter">过滤器</param>
        /// <param name="method">删除时执行的委托</param>
        public static void RemoveElementsNoReturn<TSource>(
            this IList<TSource> source, Func<TSource, bool> filter, Action<TSource> method = null)
        {
            var indexs = (from d in source where filter(d) select source.IndexOf(d)).ToList();
            indexs.Sort();
            for (var i = indexs.Count - 1; i >= 0; i--)
            {
                if (source.Count <= indexs[i])
                    continue;
                method?.Invoke(source[indexs[i]]);
                if (source.Count <= indexs[i])
                    continue;
                source.RemoveAt(indexs[i]);
            }
        }

        public static void RemoveElementsWithValue<K, V>(
            this IDictionary<K, V> source, Func<V, bool> filter, Action<K> method = null)
        {
            var indexs = (from d in source where filter(d.Value) select d.Value).ToList();

            for (var i = indexs.Count - 1; i >= 0; i--)
            {
                var key = source.FirstOrDefault(d => d.Value.Equals(indexs[i])).Key;
                if (method != null)
                {
                    method(key);
                }

                source.Remove(key);
            }
        }

        public static void Set<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        public static object Set(this IDictionary<string, object> dict, string key, object oldValue, Type type = null)
        {
            object value;
            object data = null;
            if (dict == null)
            {
                return oldValue;
            }
            if (type == null)
            {
                if (oldValue != null)
                {
                    type = oldValue.GetType();
                }
            }
            if (type == null)
            {
                return null;
            }
            if (dict.TryGetValue(key, out data))
            {
                if (type.IsEnum)
                {
                    if (data is int)
                    {
                        var index = Convert.ChangeType(data, typeof (int));
                        return (index);
                    }
                    else
                    {
                        var item = data.ToString();
                        var index = Enum.Parse(type, item);
                        return (index);
                    }
                }
                if (type == typeof (XFrmWorkAttribute))
                {
                    var item = (string) data;
                    var newone = PluginProvider.GetPlugin(item);
                    return newone;
                }

                if (data == null)
                {
                    return oldValue;
                }
                try
                {
                    value = Convert.ChangeType(data, type);
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error("字典序列化失败" + ex);
                    return null;
                }

                return value;
            }

            return oldValue;
        }


        public static T Set<T>(this IDictionary<string, object> dict, string key, T oldValue)
        {
            T value;
            object data = null;
            if (dict == null)
            {
                return oldValue;
            }
            if (dict.TryGetValue(key, out data))
            {
                var type = typeof (T);
                if (type.IsEnum)
                {
                    if (data is int)
                    {
                        var index = (T) Convert.ChangeType(data, typeof (int));
                        return (index);
                    }
                    else
                    {
                        var item = data.ToString();
                        var index = (T) Enum.Parse(typeof (T), item);
                        return (index);
                    }
                }
                if (type == typeof (XFrmWorkAttribute))
                {
                    var item = (string) data;
                    var newone = PluginProvider.GetPlugin(item);
                    return (T) Convert.ChangeType(newone, typeof (XFrmWorkAttribute));
                }

                if (data == null)
                {
                    return oldValue;
                }
                try
                {
                    value = (T) Convert.ChangeType(data, typeof (T));
                }
                catch (Exception ex)
                {
                    XLogSys.Print.Error("字典序列化失败" + ex);
                    return default(T);
                }

                return value;
            }

            return oldValue;
        }

        public static List<T> SetArray<T>(this IDictionary<string, object> dict, string key, List<T> oldValue)
        {
            object data = null;

            if (dict.TryGetValue(key, out data))
            {
                var list = data as IList;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        try
                        {
                            var value = (T) Convert.ChangeType(item, typeof (T));
                            oldValue.Add(value);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    return oldValue;
                }
                var str = data.ToString();
                if (str == "[]")
                {
                    return oldValue;
                }
                if (str[0] != '[')
                {
                    return oldValue;
                }
                return JsonConvert.Import<List<T>>(str);
            }
            return oldValue.ToList();
        }

        public static void SetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        /// <summary>
        ///     Converts a DataColumn to a double[] array.
        /// </summary>
        public static T[] ToArray<T>(this IList<IFreeDocument> column, string name)
        {
            var m = new T[column.Count];

            for (var i = 0; i < m.Length; i++)
            {
                m[i] = (T) Convert.ChangeType(column[i][name], typeof (T));
            }

            return m;
        }

        public static void UnsafeDictDeserialize(this object item, IDictionary<string, object> dict)
        {
            var type = item.GetType();
            if (type != lastType)
            {
                propertys =
                    type.GetProperties().Where(
                        d => d.CanRead && d.CanWrite && AttributeHelper.IsPOCOType(d.PropertyType)).ToArray();
            }
            lastType = type;

            foreach (var propertyInfo in propertys)
            {
                propertyInfo.SetValue(
                    item,
                    dict.Set(propertyInfo.Name, propertyInfo.GetValue(item, null), propertyInfo.PropertyType),
                    null);
            }
        }

        public static IEnumerable<int> Range(int start, Func<int, bool> contineFunc, int interval)
        {
            for (var i = start; contineFunc(i); i += interval)
            {
                yield return i;
            }
        }

        public static FreeDocument UnsafeDictSerialize(this object item)
        {
            var type = item.GetType();
            if (type != lastType)
            {
                propertys =
                    type.GetProperties().Where(
                        d => d.CanRead && d.CanWrite && AttributeHelper.IsPOCOType(d.PropertyType)).ToArray();
            }
            lastType = type;

            var doc = new FreeDocument();
            foreach (var propertyInfo in propertys)
            {
                var v = propertyInfo.GetValue(item, null);
                if (v != null)
                    doc.Add(propertyInfo.Name, v);
            }

            return doc;
        }

        #endregion
    }
}