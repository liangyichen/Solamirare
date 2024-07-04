namespace Solamirare
{
    internal class ConstValues
    {

        internal static string Failure_Format_Of_Source_Value;

        internal static string KEY_Or_IV_Can_Not_Be_Empty;

        internal static string UnSupport_Method;

        internal static string Source_Can_Not_Be_Empty;

        internal static string Path_Can_Not_Be_Empty;

        public static string DefaultUserAgent { get; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.67 Safari/537.36";


        static ConstValues()
        {
            Path_Can_Not_Be_Empty = "Path Can Not Be Null or Empty.";

            Source_Can_Not_Be_Empty = "source can not be empty or null.";

            UnSupport_Method = "Un Support Method.";

            KEY_Or_IV_Can_Not_Be_Empty = "key or iv can not be empty.";

            Failure_Format_Of_Source_Value = "failure format of source value.";
        }
    }
}
