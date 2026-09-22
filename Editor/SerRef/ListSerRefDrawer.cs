using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace RiseOn.Serializables.Editor {
    public class ListSerRefDrawer<TValue> : OdinValueDrawer<ListSerRef<TValue>> where TValue : class {
        protected override void DrawPropertyLayout(GUIContent label) {
            Property.Children[nameof(ListSerRef<TValue>.items)].Draw(label);
        }
    }
}