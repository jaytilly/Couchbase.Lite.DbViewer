using System;
using System.Collections.Generic;
using System.Linq;

namespace DbViewer.Shared.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection) => (collection == null || !collection.Any());
        public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> collection) => (collection != null && collection.Any());
    }
}
