using System;
using System.Collections.Generic;
using System.Linq;
using Hawk.Standard.Utils;
using Hawk.Standard.Utils.Plugins;

namespace Hawk.Standard.Crawlers
{

    public class BuffHelper<T>
    {
      
        private int maxBufferCount;

        [LocalizedDisplayName("key_159")]
        public bool EnableBuffer { get; set; }

        public BuffHelper (int size)
        {
            EnableBuffer = true;
            maxBufferCount = size;
            bufferDictionary=new  Dictionary<string, T>();
        }

        public void Clear()
        {
            bufferDictionary.Clear();
        }

        private T GetClone( T result)
        {
            if (result is IFreeDocument )
            {
                return (T)((result as IFreeDocument).Clone());
            }
            else if (result is List<FreeDocument>)
            {
               var r=( result as List<FreeDocument> ).Select(d => d.Clone()).ToList() ;
                return (T)Convert.ChangeType(r,typeof(T));
            }
            return result;

        }

        public T GetOrCreate(string item,T defaultValue)
        {
            if (EnableBuffer == false || bufferDictionary.Count >= maxBufferCount)
                return default(T);
            if (bufferDictionary.ContainsKey(item))
            {
                var result = bufferDictionary[item];

                return GetClone(result);
            }
            var result2= defaultValue;
            bufferDictionary[item] = result2;
            return result2;

        }
        public T Get(string item)
        {
            if (EnableBuffer == false || bufferDictionary.Count >= maxBufferCount)
                return default(T);
            if (bufferDictionary.ContainsKey(item))
            {
                 var result= bufferDictionary[item];
              
                return  GetClone(result);
            }
               
            return default(T);
        }
        public bool Set(string key ,T value)
        {
            if (EnableBuffer == false|| bufferDictionary.Count >= maxBufferCount)
                return false;
            value = GetClone(value);
            bufferDictionary.Set(key, value);
            return true;
        }

       private Dictionary<string,T> bufferDictionary { get; set; }
    }
}
