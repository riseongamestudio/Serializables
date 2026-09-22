using System;
using System.Collections;
using System.Collections.Generic;
using RiseOn.Utils;
using UnityEngine;

namespace RiseOn.Serializables {
    [Serializable]
    [ForwardAttributesTo(nameof(_Values))]
    public class EnumMap<TKey, TValue> :
        IReadOnlyDictionary<TKey, TValue>
      , ISerializationCallbackReceiver
        where TKey : struct, Enum {
        [SerializeField] internal TKey[]                _Keys;
        [SerializeField] internal EnumMapItem<TValue>[] _Values;

        private Dictionary<TKey, int> _HashedKeys = new();

        public int             Count  => _Keys.Length;
        public KeyCollection   Keys   => new(_Keys);
        public ValueCollection Values => new(_Values);

        IEnumerable<TKey>   IReadOnlyDictionary<TKey, TValue>.Keys   => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public EnumMap() {
            _Keys   = (TKey[])Enum.GetValues(typeof(TKey));
            _Values = new EnumMapItem<TValue>[_Keys.Length];
            ReHashKeys();

            #if UNITY_EDITOR
            _KeyNames = new string[_Keys.Length];
            for (int i = 0; i < _Keys.Length; ++i)
                _KeyNames[i] = _Keys[i].ToString();
            #endif
        }

        public EnumMap(IReadOnlyCollection<KeyValuePair<TKey, TValue>> source) : this() {
            foreach (var (key, value) in source) {
                if (!_HashedKeys.ContainsKey(key)) throw new InvalidKeyException();
                this[key] = value;
            }
        }

        public Dictionary<TKey, TValue> ToDictionary() {
            var dict = new Dictionary<TKey, TValue>();
            for (int i = 0; i < Count; ++i) dict.Add(_Keys[i], _Values[i]);
            return dict;
        }

        public bool ContainsKey(TKey key) => true; // Yes, of course.

        public bool TryGetValue(TKey key, out TValue value) {
            try {
                value = this[key];
                return true;
            } catch {
                value = default;
                return false;
            }
        }

        public TValue this[TKey key] {
            get {
                SafetyHashKey(key);

                return _Values[_HashedKeys[key]];
            }
            set {
                SafetyHashKey(key);

                _Values[_HashedKeys[key]] = new(value);
            }
        }

        private void ReHashKeys() {
            _HashedKeys.Clear();
            for (int i = 0; i < _Keys.Length; ++i)
                _HashedKeys.Add(_Keys[i], i);
        }

        private void SafetyHashKey(TKey key) {
            if (_HashedKeys == null
             || !_HashedKeys.ContainsKey(key))
                ReHashKeys();
        }

        [SerializeField] internal string[] _KeyNames;
        [SerializeField] internal bool     _IsKeyChanged;
        
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            if (_IsKeyChanged || _HashedKeys.Count != _Keys.Length) {
                ReHashKeys();
                _IsKeyChanged = false;
            }
        }

        public Enumerator GetEnumerator() => new(_Keys, _Values);
        
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
            private readonly TKey[]                keys;
            private readonly EnumMapItem<TValue>[] values;

            private int index;

            internal Enumerator(TKey[] keys, EnumMapItem<TValue>[] values) {
                this.keys   = keys;
                this.values = values;
                index       = -1;
            }

            public KeyValuePair<TKey, TValue> Current => new(keys[index], values[index]);

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++index < keys.Length;

            public void Dispose() { }

            void IEnumerator.Reset() => index = -1;
        }

        public readonly struct KeyCollection : IReadOnlyCollection<TKey> {
            private readonly TKey[] keys;

            internal KeyCollection(TKey[] keys) => this.keys = keys;

            public int Count => keys.Length;

            public Enumerator GetEnumerator() => new(keys);

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<TKey> {
                private readonly TKey[] keys;

                private int index;

                internal Enumerator(TKey[] keys) {
                    this.keys = keys;
                    index     = -1;
                }

                public TKey Current => keys[index];

                object IEnumerator.Current => Current;

                public bool MoveNext() => ++index < keys.Length;

                public void Dispose() { }

                void IEnumerator.Reset() => index = -1;
            }
        }

        public readonly struct ValueCollection : IReadOnlyCollection<TValue> {
            private readonly EnumMapItem<TValue>[] values;

            internal ValueCollection(EnumMapItem<TValue>[] values) => this.values = values;

            public int Count => values.Length;

            public Enumerator GetEnumerator() => new(values);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<TValue> {
                private readonly EnumMapItem<TValue>[] values;

                private int index;

                internal Enumerator(EnumMapItem<TValue>[] values) {
                    this.values = values;
                    index       = -1;
                }

                public TValue Current => values[index].Value;

                object IEnumerator.Current => Current;

                public bool MoveNext() => ++index < values.Length;

                public void Dispose() { }

                void IEnumerator.Reset() => index = -1;
            }
        }

        public class InvalidKeyException : Exception {
            public InvalidKeyException() : base("RiseOn EnumMap: Given key is not correspond to true enum key") { }
        }
    }
}