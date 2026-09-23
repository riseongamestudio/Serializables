using System;
using System.Collections.Generic;

namespace RiseOn.Serializables {
    public static class ListSerRefUtils {
        public static ListSerRef<TValue> ToListSerRef<TValue>(this IEnumerable<TValue> source) where TValue : class {
            if (source is null) throw new ArgumentNullException(nameof(source));

            var result = new ListSerRef<TValue>();
            foreach (var item in source) result.Add(item);

            return result;
        }
    }
}