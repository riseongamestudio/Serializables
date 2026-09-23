using System;
using RiseOn.Utils;

namespace RiseOn.Serializables {
    /// <summary>
    /// One saved key of an <see cref="EnumMap{TKey,TValue}"/>: the enum value and its name at the time it was written, so the map can still find it after the enum changes.
    /// </summary>
    [Serializable]
    [ForwardAttributesTo(nameof(value))]
    internal struct EnumMapEntry<TKey, TValue> where TKey : struct, Enum {
        public TKey   key;
        public string name;
        public TValue value;

        public EnumMapEntry(TKey key, string name, TValue value) {
            this.key   = key;
            this.name  = name;
            this.value = value;
        }
    }
}
