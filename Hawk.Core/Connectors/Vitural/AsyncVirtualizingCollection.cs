using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace Hawk.Core.Connectors.Vitural
{
    /// <summary>
    /// Derived VirtualizatingCollection, performing loading asychronously.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    public class AsyncVirtualizingCollection<T> : VirtualizingCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingCollection&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider)
            : base(itemsProvider)
        {
            this._synchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingCollection&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
            : base(itemsProvider, pageSize)
        {
            this._synchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingCollection&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageTimeout">The page timeout.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize, int pageTimeout)
            : base(itemsProvider, pageSize, pageTimeout)
        {
            this._synchronizationContext = SynchronizationContext.Current;
        }

        #endregion

        #region SynchronizationContext

        private readonly SynchronizationContext _synchronizationContext;

        /// <summary>
        /// Gets the synchronization context used for UI-related operations. This is obtained as
        /// the current SynchronizationContext when the AsyncVirtualizingCollection is created.
        /// </summary>
        /// <value>The synchronization context.</value>
        protected SynchronizationContext SynchronizationContext
        {
            get { return this._synchronizationContext; }
        }

        #endregion

        #region INotifyCollectionChanged



       

        /// <summary>
        /// Fires the collection reset event.
        /// </summary>
        private void FireCollectionReset()
        {
            NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            this.OnCollectionChanged(e);
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="E:PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler h = this.PropertyChanged;
            if (h != null)
                h(this, e);
        }

        /// <summary>
        /// Fires the property changed event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void FirePropertyChanged(string propertyName)
        {
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
            this.OnPropertyChanged(e);
        }

        #endregion

        #region IsLoading

        private bool _isLoading;

        /// <summary>
        /// Gets or sets a value indicating whether the collection is loading.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this collection is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoading
        {
            get
            {
                return this._isLoading;
            }
            set
            {
                if ( value != this._isLoading )
                {
                    this._isLoading = value;
                }
                this.FirePropertyChanged("IsLoading");
            }
        }

        #endregion

        #region Load overrides

        /// <summary>
        /// Asynchronously loads the count of items.
        /// </summary>
        protected override void LoadCount()
        {
            this.Count = 0;
            this.IsLoading = true;
            ThreadPool.QueueUserWorkItem(this.LoadCountWork);
        }

        /// <summary>
        /// Performed on background thread.
        /// </summary>
        /// <param name="args">None required.</param>
        private void LoadCountWork(object args)
        {
            int count = this.FetchCount();
            this.SynchronizationContext.Send(this.LoadCountCompleted, count);
        }

        /// <summary>
        /// Performed on UI-thread after LoadCountWork.
        /// </summary>
        /// <param name="args">Number of items returned.</param>
        private void LoadCountCompleted(object args)
        {
            this.Count = (int)args;
            this.IsLoading = false;
            this.FireCollectionReset();
        }

        /// <summary>
        /// Asynchronously loads the page.
        /// </summary>
        /// <param name="index">The index.</param>
        protected override void LoadPage(int index)
        {
            this.IsLoading = true;
            ThreadPool.QueueUserWorkItem(this.LoadPageWork, index);
        }

        /// <summary>
        /// Performed on background thread.
        /// </summary>
        /// <param name="args">Index of the page to load.</param>
        private void LoadPageWork(object args)
        {
            int pageIndex = (int)args;
            IList<T> page = this.FetchPage(pageIndex);
            this.SynchronizationContext.Send(this.LoadPageCompleted, new object[]{ pageIndex, page });
        }

        /// <summary>
        /// Performed on UI-thread after LoadPageWork.
        /// </summary>
        /// <param name="args">object[] { int pageIndex, IList(T) page }</param>
        private void LoadPageCompleted(object args)
        {
            int pageIndex = (int)((object[]) args)[0];
            IList<T> page = (IList<T>)((object[])args)[1];

            this.PopulatePage(pageIndex, page);
            this.IsLoading = false;
            this.FireCollectionReset();
        }

        #endregion
    }
}
