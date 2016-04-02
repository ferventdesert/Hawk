using System;
using System.Windows;
using System.Windows.Data;

namespace Hawk.Core.Utils.MVVM
{
    public class BindingEvaluator
    {
        #region Fields

        static readonly DependencyProperty DummyProperty = DependencyProperty.RegisterAttached("Dummy", typeof(object), typeof(BindingEvaluator));

        #endregion Fields

        #region Methods

        public static object GetValue(object obj, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            return GetValue(obj, new Binding(path));
        }

        static object GetValue(object obj, Binding binding)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (binding == null)
            {
                return null;
                //  throw new ArgumentNullException("binding");
            }

            binding.Source = obj;
            var dummy = new DependencyObject();
            BindingOperations.SetBinding(dummy, DummyProperty, binding);
            return dummy.GetValue(DummyProperty);
        }

        #endregion Methods
    }


}