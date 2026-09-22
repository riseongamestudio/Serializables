using System;

namespace RiseOn.Serializables {
    /// <summary>
    /// Just display single value.<br/>
    /// Use when your enum just have one key, and you don't want to see full collections display.<br/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayValueWhenSingleKeyAttribute : Attribute {
        public string Label { get; private set; }
        public DisplayValueWhenSingleKeyAttribute(string label = null) { Label = label; }
    }
}