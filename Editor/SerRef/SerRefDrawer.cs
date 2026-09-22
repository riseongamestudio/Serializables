using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace RiseOn.Serializables.Editor {
    public class SerRefDrawer<TValue> : OdinValueDrawer<SerRef<TValue>> where TValue : class {
        protected override void DrawPropertyLayout(GUIContent label) {
            Property.Children[nameof(SerRef<TValue>.value)].Draw(label);
        }
    }
}