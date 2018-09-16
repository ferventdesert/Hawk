using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hawk.Base.Utils.Plugins;

namespace Hawk.Base.Utils
{
    public class DisposeHelper : IDisposable
    {
        private readonly Action action;

        public DisposeHelper(Action action2)
        {
            action = action2;
        }

        public void Dispose()
        {
            action();
        }
    }

    public class ScriptHelper
    {
        public static FreeDocument ToDocument(dynamic obj)

        {
            var free = new FreeDocument();
            if (obj is IEnumerable)
            {
                foreach (var item in obj)
                {
                    free.Add(item.Key, item.Value);
                }
                return free;
            }
            return free;
        }

        public static List<FreeDocument> ToDocuments(dynamic obj)

        {
            var documents = new List<FreeDocument>();
            if (obj is IEnumerable)
            {
                foreach (var value in obj)
                {
                    var free = new FreeDocument();
                    if (value is IEnumerable)
                    {
                        foreach (var item in value)
                        {
                            if (item is string)
                            {
                                free.Add(item, value[item]);
                            }
                            else
                                free.Add(item.Key, item.Value);
                        }
                    }

                    documents.Add(free);
                }
            }

            return documents;
        }
        public static String RemoveSpecialCharacter(String hexData)
        {
            return Regex.Replace(hexData, "[ \\[ \\] \\^ \\-*×――(^)$%~!@#$…&%￥—+=<>《》!！??？:：•`·、。，；,.;\"‘’“”-]", "").ToUpper();
        }
    }


    public static class ExtendMethods
    {
        /// <summary>
        ///     添加一个新实例
        /// </summary>
        /// <param name="value">要添加的类型</param>
        public static T Add<T>(this ICollection<T> collection, Type value)
        {
            var instance = (T) Activator.CreateInstance(value);
            collection.Add(instance);
            return instance;
        }



        /// <summary>
        ///     添加一个新实例
        /// </summary>
        /// <param name="value">要添加的类型</param>
        /// <param name="args">自动添加的数据参数 </param>
        public static void Add<T>(this ICollection<T> collection, Type value, params object[] args)
        {
            var instance = (T) Activator.CreateInstance(value, args);
            collection.Add(instance);
        }

      
      

        public static IEnumerable<FreeDocument> CacheDo(this IEnumerable<FreeDocument> documents,IList<FreeDocument> cache=null,int maxCount=100 )
        {
            if (cache == null||cache.Count==0)
            {

                foreach (var document in documents)
                {
                    if(cache?.Count<maxCount)
                        cache.Add(document.Clone());
                    yield return document;
                }
                yield break;
            }
            else
            {
                foreach (var item in cache)
                {
                    yield return item;
                }
                yield break;
            
            }

        }

       

        public static IEnumerable<T> BatchDo<T>(this IEnumerable<T> documents,
            Func<T,bool> initFunc, Action<List<T> > batchFunc, Action endFunc = null, int batchMount = 100) 
        {
            
            var i = 0;
            var list = new List<T>();
            foreach (var document in documents)
            {
                if(document==null)
                    continue;

// list.Add(document.Clone());
                list.Add(document);
                if (list.Count == batchMount)
                {
                    try
                    {
                        batchFunc(list);
                    }
                    catch (Exception ex)
                    {
                        //XLogSys.Print.Warn(GlobalHelper.Get("key_111")+ ex.Message);
                    }

                    list = new List<T>();
                }
                yield return document;
                i++;
            }
            if (list.Count != 0)
                batchFunc(list);
            endFunc?.Invoke();
        }
    }
}