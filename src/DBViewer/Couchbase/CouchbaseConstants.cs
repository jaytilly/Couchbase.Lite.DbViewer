namespace DbViewer.Couchbase
{
    internal class CouchbaseConstants
    {
#if __IOS__
        [NotNull]
        internal const string DllName = "@rpath/LiteCore.framework/LiteCore";
#else
        internal const string DllName = "LiteCore";
#endif

        internal static readonly string ObjectTypeProperty = "@type";

        internal static readonly string ObjectTypeBlob = "blob";

        internal static readonly string C4LanguageDefault = null;

        internal static readonly string C4LanguageNone = "";

        internal static readonly string C4PlaceholderValue = "*";
    }
}
