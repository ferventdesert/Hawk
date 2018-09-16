using System;

namespace Hawk.Base.Utils.Plugins
{
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct |
        AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property |
        AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter |
        AttributeTargets.Delegate | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter)]
    public class LocalizedDescriptionAttribute : Attribute
    {
        public LocalizedDescriptionAttribute(string key)
        {
        }

        public string Description { get; set; }

        private static string Localize(string key)
        {
            // TODO: lookup from resx, perhaps with cache etc
            return key;
        }

        //public LocalizedDescriptionAttribute(string key)
        //    : base(Localize(key))
        //{

        //}
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    public class LocalizedDisplayNameAttribute : Attribute
    {
        public LocalizedDisplayNameAttribute(string key)
        {
        }

        private static string Localize(string key)
        {
            // TODO: lookup from resx, perhaps with cache etc
            return key;
        }

        //public LocalizedDisplayNameAttribute(string key)
        //    : base(Localize(key))
        //{
        //}
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    public class BrowsableAttribute : Attribute
    {
        public BrowsableAttribute(bool key)
        {

        }

        [AttributeUsage(
            AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct |
            AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property |
            AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter |
            AttributeTargets.Delegate | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter)]
        public class LocalizedCategoryAttribute : Attribute
        {
            public LocalizedCategoryAttribute(string key)
            {
            }

            //public LocalizedCategoryAttribute(string key) : base(key) { }
            //protected override string GetLocalizedString(string value)
            //{
            //    // TODO: lookup from resx, perhaps with cache etc
            //    return  value;
            //}
        }


        /// <summary>
        /// Specifies the order of property.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
        public sealed class PropertyOrderAttribute : Attribute
        {
            /// <summary>
            /// Gets or sets the order.
            /// </summary>
            /// <value>The order.</value>
            public int Order { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="PropertyOrderAttribute"/> class.
            /// </summary>
            /// <param name="order">The order.</param>
            public PropertyOrderAttribute(int order)
            {
                this.Order = order;
            }
        }

        /// <summary>
        /// Controls Browsable state of the property without having access to property declaration or inherited property.
        /// Supports a "*" (All) wildcard determining whether all the properties within the given class should be Browsable.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
        public sealed class BrowsablePropertyAttribute : Attribute
        {
            /// <summary>
            /// Determines a wildcard for all properties to be affected.
            /// </summary>
            public const string All = "*";

            /// <summary>
            /// Gets the name of the property.
            /// </summary>
            /// <value>The name of the property.</value>
            public string PropertyName { get; private set; }

            /// <summary>
            /// Gets or sets a value indicating whether property is browsable.
            /// </summary>
            /// <value><c>true</c> if property should be displayed at run time; otherwise, <c>false</c>.</value>
            public bool Browsable { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="BrowsablePropertyAttribute"/> class.
            /// </summary>
            /// <param name="propertyName">Name of the property.</param>
            /// <param name="browsable">if set to <c>true</c> the property is browsable.</param>
            public BrowsablePropertyAttribute(string propertyName, bool browsable)
            {
                this.PropertyName = string.IsNullOrEmpty(propertyName) ? All : propertyName;
                this.Browsable = browsable;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="BrowsablePropertyAttribute"/> class.
            /// </summary>
            /// <param name="propertyName">Name of the property.</param>
            public BrowsablePropertyAttribute(string propertyName) : this(propertyName, true)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="BrowsablePropertyAttribute"/> class.
            /// </summary>
            /// <param name="browsable">if set to <c>true</c> all public properties are browsable; otherwise hidden.</param>
            public BrowsablePropertyAttribute(bool browsable) : this(All, browsable)
            {
            }
        }
    }
}
