namespace Hawk.Core.Connectors
{
    public class DataTypeConverter
    {
        public static string ToType(object value)
        {
            if (value == null)
                return "text";
            var type = value.GetType().Name;
            switch (type)
            {
                case "String":
                    return "text";
                case "DateTime":
                    return "DateTime";
                case "Int32":

                    return "INT";
                case "Int64":
                case "Long":
                    return "Long";
                case "Float":
                case "Double":
                    return "DOUBLE";
                default:
                    return "text";

            }

        }
    }
}
