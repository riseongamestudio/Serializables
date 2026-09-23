using System;

namespace RiseOn.Serializables {
    /// <summary>
    /// On an <see cref="EnumMap{TKey,TValue}"/> field whose enum has a single member: draws that one value as a plain field instead of the whole map.<br/>
    /// <see cref="Label"/> replaces the field label when set.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FlattenSingleKeyAttribute : Attribute {
        public string Label { get; }

        public FlattenSingleKeyAttribute(string label = null) {
            Label = label;
        }
    }
}
