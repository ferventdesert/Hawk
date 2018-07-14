using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hawk.Core.Connectors;

namespace Hawk.Core.Utils.Plugins
{
    [XFrmWork("自由文档",  "可存储键值对形式的自由文档", "")]
    public partial class FreeDocument : IFreeDocument, IDictionary<string, object>
    {
        #region Constructors and Destructors

        public FreeDocument()
        {
            DataItems = new Dictionary<string, object>();
            Name = "Doc";
            IsNullOfNotExist = true;
        }

        public FreeDocument(IDictionary<string, object> dictionary)
        {
            DataItems = dictionary;
            Name = "Doc";
            IsNullOfNotExist = true;
        }

        #endregion

        #region Properties

        public string Name { get; set; }

        /// <summary>
        ///     子元素，注意!不能在构造函数里显式初始化，否则会造成循环初始化而出错
        /// </summary>
        public List<FreeDocument> Children { get; set; }

        public IEnumerable<object> BindingValuePairs
        {
            get
            {
                foreach (var dataItem in DataItems)
                {
                    if (dataItem.Value is IDictionarySerializable)
                        yield return dataItem.Value;
                    else
                    {
                        yield return dataItem.Key + ":  " + dataItem.Value;
                    }
                }
                if (Children == null)
                    yield break;

                for (int index = 0; index < Children.Count; index++)
                {
                    FreeDocument freeDocument = Children[index];
                    yield return freeDocument;
                }
            }
        }

        public int Count
        {
            get { return DataItems.Count; }
        }

        public IDictionary<string, object> DataItems { get; set; }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection<string> Keys
        {
            get { return DataItems.Keys; }
        }


        public IEnumerable<string> PropertyNames
        {
            get { return DataItems.Keys; }
        }

        public int CompareTo(object obj)
        {
            var ifree = obj as IFreeDocument;
            int v2;
            int v1 =
                Values.Where(d => d != null).Aggregate((a, b) => a.ToString() + b.ToString()).ToString().GetHashCode();
            if (ifree != null)
                v2 =
                    ifree.Values.Where(d => d != null)
                        .Aggregate((a, b) => a.ToString() + b.ToString())
                        .ToString()
                        .GetHashCode();
            else
            {
                v2 = obj.GetHashCode();
            }
            return v1 - v2;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ICollection<object> Values
        {
            get { return DataItems.Values; }
        }

        public override string ToString()
        {
            return string.Format("{0}, Child:{1}, Items:{2}", Name, Children == null ? 0 : Children.Count,
                DataItems.Count);
        }

        #endregion

        #region Indexers

        public virtual bool IsNullOfNotExist { get; set; }

        public virtual FreeDocument this[int key]
        {
            get
            {
                if (Children == null || Children.Count < key - 1)
                {
                    return null;
                }
                return Children[key];
            }
            set
            {
                if (Children == null || Children.Count < key - 1)
                {
                }
                else
                {
                    Children[key] = value;
                }
            }
        }

        public virtual object this[string key]
        {
            get
            {
                object v;
                if (DataItems.TryGetValue(key, out v))
                {
                    return v;
                }
                return "";
            }
            set { DataItems.SetValue(key, value); }
        }

        #endregion

        #region Implemented Interfaces

        #region ICollection<KeyValuePair<string,object>>

        public void Add(KeyValuePair<string, object> item)
        {
            DataItems.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            DataItems.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return DataItems.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            DataItems.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return DataItems.Remove(item.Key);
        }

        #endregion

        #region IDictionary<string,object>

        public void Add(string key, object value)
        {
            DataItems.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return DataItems.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return DataItems.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return DataItems.TryGetValue(key, out value);
        }

        #endregion

        #region IDictionarySerializable

        public virtual void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            foreach (var dict in dicts)
            {
                if (DataItems.ContainsKey(dict.Key))
                {
                    DataItems[dict.Key] = dict.Value;
                }
                else
                {
                    DataItems.Add(dict.Key, dict.Value);
                }
            }
            var fre = dicts as FreeDocument;
            if (fre?.Children != null)
            {
                Children = fre.Children;
            }
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            if (scenario == Scenario.Binding)
            {
                var dictionary = new FreeDocument();
                //     dictionary.Add("Name", "Name");
                foreach (var item in DataItems)
                {
                    string str = string.Format("DataItems[{0}]", item.Key);
                    dictionary.Add(string.Format("[{0}]", item.Key), str);
                }

                return
                    dictionary;
            }
            //var dict = new FreeDocument();
            //foreach (var dataItem in DataItems)
            //{
            //    dict.Add(dataItem.Key,dataItem.Value);

            //}
            //dict.Children = this.Children;
            return this;
        }

        #endregion

        #region IEnumerable

        public string GetJson()
        {
            return "hahaha";
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,object>>

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            //  yield return new KeyValuePair<string, object>("Name", Name);

            bool hasChild = false;
            foreach (var dataItem in DataItems)
            {
                if (dataItem.Key == "Children")
                    hasChild = true;
                yield return dataItem;
            }
            if (Children != null&&hasChild==false)
                yield return new KeyValuePair<string, object>("Children", Children);
        }

        #endregion

        #endregion
    }
}