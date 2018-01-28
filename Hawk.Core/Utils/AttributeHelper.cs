using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Utils
{
    public enum EncodingType
    {
        UTF8,
        GB2312,
        ASCII,
        UTF16,
        UTF7,
        UTF32,
        BigEndianUTF8,
        Unknown
    }

    public class AttributeHelper
    {

        public static Encoding GetEncoding(EncodingType type)
        {
            switch (type)
            {
                case EncodingType.UTF8:
                    return new UTF8Encoding();
                case EncodingType.GB2312:
                    return Encoding.GetEncoding("gb2312");
                case EncodingType.ASCII:
                    return new ASCIIEncoding();
                case EncodingType.Unknown:
                    return new UTF8Encoding();
                    break;
                default:
                    return null;
            }
        }
        #region Public Methods

        public static T FromInt<T>(int value)
        {
            return (T)(object)value;
        }

        public static T FromString<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }


        public static bool IsNumeric(object item)
        {
            bool t = item is int || item is long || item is short || item is byte;
            if (t)
                return true;
            try
            {
                double t2 = Convert.ToDouble(item);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public static bool IsPOCOType(Type type)
        {
            if (type.IsEnum)
                return true;
            return type == typeof(int) || type == typeof(bool) || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(double) ||   type == typeof(Single) || type == typeof(string) || type == typeof(short) ||
                   type == typeof(byte);
        }

        public static bool IsBaseType(Type a, Type baseType)
        {
            Type t = a;
            while (t.BaseType != baseType)
            {
                t = t.BaseType;
                if (t == null)
                    return false;
                if (t == typeof(object))
                    return false;
            }
            return true;
        }

        public static IEnumerable<T> GetAttributes<T>(object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            return GetAttributes<T>(target.GetType());
        }

        public static IEnumerable<T> GetAttributes<T>(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            IEnumerable<T> attributes = from T attribute in type.GetCustomAttributes(typeof(T), true) select attribute;

            return attributes;
        }

        /// <summary>
        ///     获取枚举项的Attribute
        /// </summary>
        /// <typeparam name="T">自定义的Attribute</typeparam>
        /// <param name="source">枚举</param>
        /// <returns>返回枚举,否则返回null</returns>
        public static T GetCustomAttribute<T>(Enum source) where T : Attribute
        {
            Type sourceType = source.GetType();
            string sourceName = Enum.GetName(sourceType, source);
            FieldInfo field = sourceType.GetField(sourceName);
            object[] attributes = field.GetCustomAttributes(typeof(T), false);
            return attributes.OfType<T>().FirstOrDefault();
        }

        public static XFrmWorkAttribute GetCustomAttribute(Type source)
        {
            object[] attributes = source.GetCustomAttributes(typeof(XFrmWorkAttribute), false);
            foreach (object attribute in attributes)
            {
                var xFrmWorkAttribute = attribute as XFrmWorkAttribute;
                if (xFrmWorkAttribute != null)
                {
                    xFrmWorkAttribute.MyType = source;
                    return xFrmWorkAttribute;
                }
            }

            return new XFrmWorkAttribute("不存在定义", "NULL", "NULL", "无定义资源");
        }

        /// <summary>
        ///     获取DescriptionAttribute描述
        /// </summary>
        /// <param name="source">枚举</param>
        /// <returns>有description标记，返回标记描述，否则返回null</returns>
        public static string GetDescription(Enum source)
        {
            var attr = GetCustomAttribute<DescriptionAttribute>(source);
            return attr?.Description;
        }



        public static object ConvertTo(object source, SimpleDataType targetType, ref bool success)
        {
            success = true;
            switch (targetType)
            {
                case SimpleDataType.INT:
                    try
                    {
                        return Convert.ToInt32(source);
                    }
                    catch (Exception)
                    {
                        success = false;

                        return 0;
                    }
                case SimpleDataType.BOOL:
                    try
                    {
                        return Convert.ToBoolean(source);
                    }
                    catch (Exception)
                    {
                        success = false;

                        return false;
                    }


                case SimpleDataType.DOUBLE:
                    try
                    {
                        return Convert.ToDouble(source);
                    }
                    catch (Exception)
                    {
                        success = false;
                        return 0;
                    }
                case SimpleDataType.DATETIME:
                    try
                    {
                        return Convert.ToDateTime(source);
                    }
                    catch (Exception)
                    {
                        success = false;
                        return DateTime.Now;
                    }
                case SimpleDataType.STRING:
                    {
                        try
                        {
                            return source.ToString();
                        }
                        catch (Exception)
                        {
                            success = false;
                            return "";
                        }
                    }
            }
            return null;
        }

        #endregion

        // Public Methods (7) 

        public static bool IsFloat(object item)
        {
            return item is float || item is double;
        }
    }

    public enum SimpleDataType
    {
        INVAILD = -1, //未知类型
        STRING = 0,
        BOOL = 1,
        INT = 2,
        /// <summary>
        /// 时间差
        /// </summary>
        TIMESPAN = 3,


        DOUBLE = 4,
        DATETIME = 5,
        /// <summary>
        /// 枚举
        /// </summary>
        ENUMS = 6,

        /// <summary>
        /// 数组
        /// </summary>
        ARRAY = 0x40,
        /// <summary>
        /// 字典
        /// </summary>
        DICT,
    }
}