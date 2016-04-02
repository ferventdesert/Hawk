using System;

namespace Hawk.Core.Utils
{
    public class ForceLazy<T>
    {
        #region Constants and Fields

        private readonly Func<T> func;

        private bool isLoaded;

        private T value;

        #endregion

        #region Constructors and Destructors

        public ForceLazy(Func<T> factory, bool isForceRefresh = false)
        {
            this.isLoaded = false;
            this.func = factory;
            this.IsForceRefresh = isForceRefresh;
        }

        #endregion

        #region Properties

        public bool IsForceRefresh { get; private set; }

        public T Value
        {
            get
            {
                if (this.IsForceRefresh == false && this.isLoaded)
                {
                    return this.value;
                }
                if (func == null) return default(T);
                this.value = this.func();
                this.isLoaded = true;

                return this.value;
            }
        }

        #endregion
    }
}