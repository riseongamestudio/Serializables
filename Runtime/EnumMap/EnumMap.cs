using System;
using System.Collections;
using System.Collections.Generic;
using RiseOn.Utils;
using UnityEngine;

namespace RiseOn.Serializables {
    [Serializable]
    [ForwardAttributesTo(nameof(entries))]
    public partial class EnumMap<TKey, TValue> :
        IReadOnlyDictionary<TKey, TValue>
      , ISerializationCallbackReceiver
        where TKey : struct, Enum {
        [SerializeField] internal List<EnumMapEntry<TKey, TValue>> entries;

        /// <summary>
        /// For each key of the current enum (in <see cref="EnumMapKeys{TKey}.Keys"/> order), the index of its entry in <see cref="entries"/>, or -1 when the saved data has no entry for it.<br/>
        /// Null until resolved.
        /// </summary>
        private int[] entryIndices;

        public int             Count  => EnumMapKeys<TKey>.Keys.Length;
        public KeyCollection   Keys   => new(EnumMapKeys<TKey>.Keys);
        public ValueCollection Values => new(this);

        IEnumerable<TKey>   IReadOnlyDictionary<TKey, TValue>.Keys   => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public EnumMap() {
            var keys  = EnumMapKeys<TKey>.Keys;
            var names = EnumMapKeys<TKey>.Names;

            entries      = new List<EnumMapEntry<TKey, TValue>>(keys.Length);
            entryIndices = new int[keys.Length];
            for (int i = 0; i < keys.Length; ++i) {
                entries.Add(new EnumMapEntry<TKey, TValue>(keys[i], names[i], default));
                entryIndices[i] = i;
            }
        }

        public EnumMap(IEnumerable<KeyValuePair<TKey, TValue>> source) : this() {
            foreach (var (key, value) in source) this[key] = value;
        }

        public Dictionary<TKey, TValue> ToDictionary() {
            var dict = new Dictionary<TKey, TValue>(Count);
            foreach (var (key, value) in this) dict.Add(key, value);
            return dict;
        }

        public bool ContainsKey(TKey key) {
            return true; // Yes, of course.
        }

        public bool TryGetValue(TKey key, out TValue value) {
            if (!EnumMapKeys<TKey>.Ordinals.TryGetValue(key, out var ordinal)) {
                value = default;
                return false;
            }

            value = ValueAt(ordinal);
            return true;
        }

        public TValue this[TKey key] {
            get => ValueAt(OrdinalOf(key));
            set {
                var ordinal = OrdinalOf(key);
                var index   = ResolvedEntryIndices[ordinal];

                if (index < 0) {
                    // The saved data predates this key: the first write adds its entry.
                    entryIndices[ordinal] = entries.Count;
                    entries.Add(new EnumMapEntry<TKey, TValue>(key, EnumMapKeys<TKey>.Names[ordinal], value));
                    return;
                }

                var entry = entries[index];
                entry.value    = value;
                entries[index] = entry;
            }
        }

        /// <summary>
        /// True when the saved entries are exactly the keys of the current enum, in order.
        /// </summary>
        internal bool IsUpToDate => MatchesEnum(entries);

        internal TValue ValueAt(int ordinal) {
            var index = ResolvedEntryIndices[ordinal];
            return index < 0 ? default : entries[index].value;
        }

        private int[] ResolvedEntryIndices => entryIndices ??= ResolveEntryIndices(entries ??= new());

        private static int OrdinalOf(TKey key) {
            if (!EnumMapKeys<TKey>.Ordinals.TryGetValue(key, out var ordinal)) throw new InvalidKeyException();
            return ordinal;
        }

        private static bool MatchesEnum(List<EnumMapEntry<TKey, TValue>> entries) {
            var keys  = EnumMapKeys<TKey>.Keys;
            var names = EnumMapKeys<TKey>.Names;
            if (entries == null || entries.Count != keys.Length) return false;

            var comparer = EqualityComparer<TKey>.Default;
            for (int i = 0; i < keys.Length; ++i) {
                if (entries[i].name != names[i] || !comparer.Equals(entries[i].key, keys[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// Matches the saved entries to the keys of the current enum: by name first, then by number for the keys still unmatched.<br/>
        /// The entries themselves are left untouched.
        /// </summary>
        private static int[] ResolveEntryIndices(List<EnumMapEntry<TKey, TValue>> entries) {
            var keys    = EnumMapKeys<TKey>.Keys;
            var names   = EnumMapKeys<TKey>.Names;
            var indices = new int[keys.Length];

            if (MatchesEnum(entries)) {
                for (int i = 0; i < indices.Length; ++i) indices[i] = i;
                return indices;
            }

            var used = new bool[entries.Count];

            for (int i = 0; i < keys.Length; ++i) {
                indices[i] = -1;
                for (int j = 0; j < entries.Count; ++j) {
                    if (used[j] || entries[j].name != names[i]) continue;

                    indices[i] = j;
                    used[j]    = true;
                    break;
                }
            }

            var comparer = EqualityComparer<TKey>.Default;
            for (int i = 0; i < keys.Length; ++i) {
                if (indices[i] >= 0) continue;

                for (int j = 0; j < entries.Count; ++j) {
                    if (used[j] || !comparer.Equals(entries[j].key, keys[i])) continue;

                    indices[i] = j;
                    used[j]    = true;
                    break;
                }
            }

            return indices;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        // Unity can call this off the main thread, so only drop the lookup and resolve it on first access.
        void ISerializationCallbackReceiver.OnAfterDeserialize() => entryIndices = null;

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
            private readonly EnumMap<TKey, TValue> map;

            private int index;

            internal Enumerator(EnumMap<TKey, TValue> map) {
                this.map = map;
                index    = -1;
            }

            public KeyValuePair<TKey, TValue> Current => new(EnumMapKeys<TKey>.Keys[index], map.ValueAt(index));

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++index < map.Count;

            public void Dispose() { }

            void IEnumerator.Reset() {
                index = -1;
            }
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

                void IEnumerator.Reset() {
                    index = -1;
                }
            }
        }

        public readonly struct ValueCollection : IReadOnlyCollection<TValue> {
            private readonly EnumMap<TKey, TValue> map;

            internal ValueCollection(EnumMap<TKey, TValue> map) => this.map = map;

            public int Count => map.Count;

            public Enumerator GetEnumerator() => new(map);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<TValue> {
                private readonly EnumMap<TKey, TValue> map;

                private int index;

                internal Enumerator(EnumMap<TKey, TValue> map) {
                    this.map = map;
                    index    = -1;
                }

                public TValue Current => map.ValueAt(index);

                object IEnumerator.Current => Current;

                public bool MoveNext() => ++index < map.Count;

                public void Dispose() { }

                void IEnumerator.Reset() {
                    index = -1;
                }
            }
        }

        public class InvalidKeyException : Exception {
            public InvalidKeyException() : base("RiseOn EnumMap: Given key is not correspond to true enum key") { }
        }
    }

    /// <summary>
    /// Keys and names of an enum, read once per enum type.
    /// </summary>
    internal static class EnumMapKeys<TKey> where TKey : struct, Enum {
        internal static readonly TKey[]   Keys  = (TKey[])Enum.GetValues(typeof(TKey));
        internal static readonly string[] Names = Enum.GetNames(typeof(TKey));

        internal static readonly Dictionary<TKey, int> Ordinals = CreateOrdinals();

        private static Dictionary<TKey, int> CreateOrdinals() {
            var ordinals = new Dictionary<TKey, int>(Keys.Length);
            for (int i = 0; i < Keys.Length; ++i) ordinals.Add(Keys[i], i);
            return ordinals;
        }
    }
}
