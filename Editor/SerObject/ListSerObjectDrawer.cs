using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace RiseOn.Serializables.Editor {
    public class ListSerObjectDrawer<TValue> : OdinValueDrawer<ListSerObject<TValue>> where TValue : class {
        protected override void DrawPropertyLayout(GUIContent label) {
            Property.Children[nameof(ListSerObject<TValue>.items)].Draw(label);
        }
    }
}