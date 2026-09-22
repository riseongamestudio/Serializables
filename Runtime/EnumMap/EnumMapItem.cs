using System;
using RiseOn.Utils;
using UnityEngine;

namespace RiseOn.Serializables {
    [Serializable]
    [ForwardAttributesTo(nameof(value))]
    internal struct EnumMapItem<TValue> : IEquatable<TValue> {
        [SerializeField]
        public TValue value;

        public TValue Value {
            get => value;
            set => this.value = value;
        }

        public EnumMapItem(in TValue value) => this.value = value;

        public bool Equals(TValue other) => value is null ? other is null : value.Equals(other);
        public override string ToString() => value.ToString();
        public static implicit operator TValue(EnumMapItem<TValue> value) => value.Value;
    }
}