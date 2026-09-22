using System;
using System.Collections;
using System.Collections.Generic;
using RiseOn.Utils;
using UnityEngine;

namespace RiseOn.Serializables {
    [Serializable]
    [ForwardAttributesTo(nameof(items))]
    public class ListSerRef<TValue> : IList<TValue>, IReadOnlyList<TValue> where TValue : class {
        [SerializeField]
        internal List<SerRef<TValue>> items = new();

        public int  Count      => items.Count;
        public bool IsReadOnly => false;

        public TValue this[int index] {
            get => items[index].Value;
            set => items[index] = new(value);
        }

        public void Add(TValue item) {
            items.Add(new(item));
        }

        public void Clear() {
            items.Clear();
        }

        public bool Contains(TValue item) {
            return items.Contains(new(item));
        }

        public void CopyTo(TValue[] array, int arrayIndex) {
            if (array is null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Destination array is not long enough.", nameof(array));

            for (var i = 0; i < Count; ++i) array[arrayIndex + i] = items[i].Value;
        }

        public int IndexOf(TValue item) {
            return items.IndexOf(new(item));
        }

        public void Insert(int index, TValue item) {
            items.Insert(index, new(item));
        }

        public bool Remove(TValue item) {
            return items.Remove(new(item));
        }

        public void RemoveAt(int index) {
            items.RemoveAt(index);
        }

        public Enumerator GetEnumerator() {
            return new(items);
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<TValue> {
            private readonly List<SerRef<TValue>> items;

            private int index;

            internal Enumerator(List<SerRef<TValue>> items) {
                this.items = items;
                index      = -1;
            }

            public TValue Current => items[index].Value;

            object IEnumerator.Current => Current;

            public bool MoveNext() {
                return ++index < items.Count;
            }

            public void Dispose() { }

            void IEnumerator.Reset() {
                index = -1;
            }
        }
    }
}