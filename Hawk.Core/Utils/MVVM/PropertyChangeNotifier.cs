using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Hawk.Core.Utils.MVVM
{
    public class PropertyChangeNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propName)
        {

            //this.VerifyPropertyName(propName);
            if (null != PropertyChanged)
            {
                ControlExtended.UIInvoke(() => PropertyChanged(this, new PropertyChangedEventArgs(propName)));
             
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; set; }


        public void InformPropertyChanged(string propName)
        {
            OnPropertyChanged(propName);
        }
        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    System.Diagnostics.Debug.Fail(msg);
            }
        }
    }
}
