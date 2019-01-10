using System;
using Hawk.Core.Utils;
using System.Collections;
using System.Collections.Generic;

namespace Hawk.Core.Connectors.Vitural
{
    public class EnumableVirtualCollection<T> : IItemsProvider<T>,IEnumerable<T>
    {
        private  IEnumerator<T> enumerator;
        private int currentIndex;

        private int? totalcount;
        private IEnumerable<T> enumerable; 
        public EnumableVirtualCollection(IEnumerable<T> items, int count = -1)
        {
            if (count != -1)
                totalcount = count;
            enumerable = items;
            enumerator = items.GetEnumerator();
            currentIndex = 0;
        }

        public int FetchCount()
        {
            if (totalcount == null)
            {
                return int.MaxValue;
            }
            return totalcount.Value;
        }

        public string Name
        {
            get { return GlobalHelper.Get("key_91"); }
        }

        public IList<T> FetchRange(int startIndex, int count)
        {
            if (currentIndex > startIndex)
            {
               enumerator=  enumerable.GetEnumerator();
                currentIndex = 0;
            }

            var items = new List<T>();
            while (true)
            {
                bool r = enumerator.MoveNext();
                if (currentIndex >= startIndex)
                {
                    items.Add(enumerator.Current);
                }

                currentIndex++;

                if (r == false)
                {
                    totalcount = currentIndex;
                    OnAlreadyGetSize();
                }
                if (r == false || items.Count == count)
                    return items;
            }
        }

        public event EventHandler AlreadyGetSize;

        protected virtual void OnAlreadyGetSize()
        {
            EventHandler handler = AlreadyGetSize;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public IEnumerator<T> GetEnumerator()
        {
            enumerator.Reset();
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}