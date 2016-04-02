using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Windows.Threading;

namespace Hawk.Core.Utils
{
    public class SafeObservable<T> : IList<T>, INotifyCollectionChanged
    {
        #region Constants and Fields

        private readonly IList<T> _collection = new List<T>();

        private readonly Dispatcher dispatcher;

        private readonly ReaderWriterLock sync = new ReaderWriterLock();

        #endregion

        #region Constructors and Destructors

        public SafeObservable()
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;
        }

        #endregion

        #region Events

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Properties

        public int Count
        {
            get
            {
                this.sync.AcquireReaderLock(Timeout.Infinite);
                int result = this._collection.Count;
                this.sync.ReleaseReaderLock();
                return result;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this._collection.IsReadOnly;
            }
        }

        #endregion

        #region Indexers

        public T this[int index]
        {
            get
            {
                this.sync.AcquireReaderLock(Timeout.Infinite);
                T result = this._collection[index];
                this.sync.ReleaseReaderLock();
                return result;
            }
            set
            {
                this.sync.AcquireWriterLock(Timeout.Infinite);
                if (this._collection.Count == 0 || this._collection.Count <= index)
                {
                    this.sync.ReleaseWriterLock();
                    return;
                }
                this._collection[index] = value;
                this.sync.ReleaseWriterLock();
            }
        }

        #endregion

       

        #region Implemented Interfaces

        #region ICollection<T>

        public void Add(T item)
        {
            if (Thread.CurrentThread == this.dispatcher.Thread)
            {
                this.DoAdd(item);
            }
            else
            {
                this.dispatcher.BeginInvoke((Action)(() => { this.DoAdd(item); }));
            }
        }

        public void Clear()
        {
            if (Thread.CurrentThread == this.dispatcher.Thread)
            {
                this.DoClear();
            }
            else
            {
                this.dispatcher.BeginInvoke((Action)(() => { this.DoClear(); }));
            }
        }

        public bool Contains(T item)
        {
            this.sync.AcquireReaderLock(Timeout.Infinite);
            bool result = this._collection.Contains(item);
            this.sync.ReleaseReaderLock();
            return result;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.sync.AcquireWriterLock(Timeout.Infinite);
            this._collection.CopyTo(array, arrayIndex);
            this.sync.ReleaseWriterLock();
        }

        public bool Remove(T item)
        {
            if (Thread.CurrentThread == this.dispatcher.Thread)
            {
                return this.DoRemove(item);
            }
            else
            {
                DispatcherOperation op = this.dispatcher.BeginInvoke(new Func<T, bool>(this.DoRemove), item);
                if (op == null || op.Result == null)
                {
                    return false;
                }
                return (bool)op.Result;
            }
        }

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._collection.GetEnumerator();
        }

        #endregion

        #region IEnumerable<T>

        public IEnumerator<T> GetEnumerator()
        {
            return this._collection.GetEnumerator();
        }

        #endregion

        #region IList<T>

        public int IndexOf(T item)
        {
            this.sync.AcquireReaderLock(Timeout.Infinite);
            int result = this._collection.IndexOf(item);
            this.sync.ReleaseReaderLock();
            return result;
        }

        public void Insert(int index, T item)
        {
            if (Thread.CurrentThread == this.dispatcher.Thread)
            {
                this.DoInsert(index, item);
            }
            else
            {
                this.dispatcher.BeginInvoke((Action)(() => { this.DoInsert(index, item); }));
            }
        }

        public void RemoveAt(int index)
        {
            if (Thread.CurrentThread == this.dispatcher.Thread)
            {
                this.DoRemoveAt(index);
            }
            else
            {
                this.dispatcher.BeginInvoke((Action)(() => this.DoRemoveAt(index)));
            }
        }

        #endregion

        #endregion

     

        #region Methods

        private void DoAdd(T item)
        {
            this.sync.AcquireWriterLock(Timeout.Infinite);
            this._collection.Add(item);
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(
                    this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }
            this.sync.ReleaseWriterLock();
        }

        private void DoClear()
        {
            this.sync.AcquireWriterLock(Timeout.Infinite);
            this._collection.Clear();
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            this.sync.ReleaseWriterLock();
        }

        private void DoInsert(int index, T item)
        {
            this.sync.AcquireWriterLock(Timeout.Infinite);
            this._collection.Insert(index, item);
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(
                    this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            }
            this.sync.ReleaseWriterLock();
        }

        private bool DoRemove(T item)
        {
            this.sync.AcquireWriterLock(Timeout.Infinite);
            int index = this._collection.IndexOf(item);
            if (index == -1)
            {
                this.sync.ReleaseWriterLock();
                return false;
            }
            bool result = this._collection.Remove(item);
            if (result && this.CollectionChanged != null)
            {
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            this.sync.ReleaseWriterLock();
            return result;
        }

        private void DoRemoveAt(int index)
        {
            this.sync.AcquireWriterLock(Timeout.Infinite);
            if (this._collection.Count == 0 || this._collection.Count <= index)
            {
                this.sync.ReleaseWriterLock();
                return;
            }
            this._collection.RemoveAt(index);
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            this.sync.ReleaseWriterLock();
        }

        #endregion
    }
}