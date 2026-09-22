using System;
using System.Collections.Generic;

namespace RiseOn.Serializables {
    public static class ListSerObjectExtensions {
        public static ListSerObject<TValue> ToListSerObject<TValue>(this IEnumerable<TValue> source) where TValue : class {
            if (source is null) throw new ArgumentNullException(nameof(source));

            var result = new ListSerObject<TValue>();
            foreach (var item in source) result.Add(item);

            return result;
        }
    }
}