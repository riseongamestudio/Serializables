using System;
using System.Runtime.Serialization;
using RiseOn.Utils;
using UnityEngine;

namespace RiseOn.Serializables {
    [Serializable]
    [DataContract]
    [ForwardAttributesTo(nameof(value))]
    public struct SerRef<TValue>
        : IEquatable<SerRef<TValue>>
        , IEquatable<TValue>
        where TValue : class {
        [SerializeReference]
        [DataMember(Name = nameof(value))]
        internal TValue value;

        public TValue Value => value;
        public bool IsNull => value == null;

        public SerRef(TValue value) {
            this.value = value;
        }

        public override string ToString() => IsNull ? string.Empty : value.ToString();
        public override int GetHashCode() => IsNull ? 0 : value.GetHashCode();

        public bool Equals(SerRef<TValue> other) => ReferenceEquals(value, other.value);
        public bool Equals(TValue other) => Equals(new SerRef<TValue>(other));

        public override bool Equals(object obj) => obj switch {
            SerRef<TValue> otherSerRef => Equals(otherSerRef)
          , TValue otherObj            => Equals(otherObj)
          , _                          => false
        };

        public static bool operator ==(SerRef<TValue> left, SerRef<TValue> right) => left.Equals(right);
        public static bool operator !=(SerRef<TValue> left, SerRef<TValue> right) => !(left == right);
        public static bool operator ==(SerRef<TValue> left, TValue right) => left.Equals(right);
        public static bool operator !=(SerRef<TValue> left, TValue right) => !(left == right);
        public static bool operator ==(TValue left, SerRef<TValue> right) => right.Equals(left);
        public static bool operator !=(TValue left, SerRef<TValue> right) => !(left == right);
    }
}