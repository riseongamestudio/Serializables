using System;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace RiseOn.Serializables.Editor {
    internal class EnumMapEntryDrawer<TKey, TValue> : OdinValueDrawer<EnumMapEntry<TKey, TValue>>
        where TKey : struct, Enum {
        protected override void DrawPropertyLayout(GUIContent label) {
            Property.Children[nameof(EnumMapEntry<TKey, TValue>.value)].Draw(label);
        }
    }
}
