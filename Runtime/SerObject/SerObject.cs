using System;
using System.Runtime.Serialization;
using RiseOn.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Serializables {
    [Serializable]
    [DataContract]
    [ForwardAttributesTo(nameof(value))]
    public struct SerObject<TValue>
        : IEquatable<SerObject<TValue>>
        , IEquatable<TValue>
        where TValue : class {
        [SerializeField]
        [DataMember(Name = nameof(value))]
        internal Object value;

        public TValue Value => value as TValue;
        public bool IsNull => value == null || value is not TValue;

        public SerObject(TValue value) {
            this.value = value as Object;
        }

        public override string ToString() => IsNull ? string.Empty : value.ToString();
        public override int GetHashCode() => IsNull ? 0 : value.GetHashCode();

        public bool Equals(SerObject<TValue> other) => IsNull ? other.IsNull : value == other.value;
        public bool Equals(TValue other) => Equals(new SerObject<TValue>(other));

        public override bool Equals(object obj) => obj is SerObject<TValue> other && Equals(other);

        public static bool operator ==(SerObject<TValue> left, SerObject<TValue> right) => left.Equals(right);
        public static bool operator !=(SerObject<TValue> left, SerObject<TValue> right) => !(left == right);
        public static bool operator ==(SerObject<TValue> left, TValue right) => left.Equals(right);
        public static bool operator !=(SerObject<TValue> left, TValue right) => !(left == right);
        public static bool operator ==(TValue left, SerObject<TValue> right) => right.Equals(left);
        public static bool operator !=(TValue left, SerObject<TValue> right) => !(left == right);
    }
}