using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Crawlers
{

    public class BuffHelper<T>
    {
      
        private int maxBufferCount;

        [DisplayName("启动缓存")]
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
        public void Set(string key ,T value)
        {
            if (EnableBuffer == false|| bufferDictionary.Count >= maxBufferCount)
                return;
            value = GetClone(value);
            bufferDictionary.Set(key, value);
        }

       private Dictionary<string,T> bufferDictionary { get; set; }
    }
}
