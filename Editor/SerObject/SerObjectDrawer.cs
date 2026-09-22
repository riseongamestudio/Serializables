using System;
using RiseOn.Utils.Editor;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Serializables.Editor {
    public class SerObjectDrawer<TValue> : OdinValueDrawer<SerObject<TValue>>, IDisposable where TValue : class {
        private const float PICK_BUTTON_WIDTH = 19;

        private GameObject            rootPrefab;
        private bool                  allowSceneObjects;
        private SerializedProperty    unityValueProp;
        private InlineEditorImitator  inlineEditorImitator;
        private InlineEditorAttribute inlineEditorAttr;
        private RequiredAttribute     requiredAttr;

        protected override void Initialize() {
            var targetObj = Property.Tree.WeakTargets[0] as Object;
            var valueProp = Property.Children[nameof(SerObject<TValue>.value)];

            rootPrefab           = targetObj.GetRootPrefab();
            allowSceneObjects    = !EditorUtility.IsPersistent(targetObj);
            unityValueProp       = Property.Tree.UnitySerializedObject.FindProperty(valueProp.UnityPropertyPath);
            inlineEditorImitator = new InlineEditorImitator(typeof(TValue) == typeof(GameObject), wrapper => ((SerObject<TValue>)wrapper).value);
            inlineEditorAttr     = valueProp.GetAttribute<InlineEditorAttribute>();
            requiredAttr         = valueProp.GetAttribute<RequiredAttribute>();

            if (inlineEditorAttr is { ExpandedHasValue: true }) Property.State.Expanded = true;
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            if (!DrawMismatchBoxIfNeeded()) DrawRequiredBoxIfNeeded();

            if (inlineEditorAttr == null) DrawStandardField(EditorGUILayout.GetControlRect(hasLabel: false), label);
            else inlineEditorImitator.DrawLayout(Property, ValueEntry, inlineEditorAttr, label ?? Property.Label ?? GUIContent.none, DrawStandardField);
        }

        private void DrawStandardField(Rect position, GUIContent label) {
            HandlePickButton(position);

            var newLabel = EditorGUI.BeginProperty(position, label, unityValueProp);
            EditorGUI.BeginChangeCheck();

            var newValue = SirenixEditorFields.UnityObjectField(
                position
              , label == null ? null : newLabel
              , ValueEntry.SmartValue.value
              , typeof(TValue)
              , allowSceneObjects);

            if (EditorGUI.EndChangeCheck()) ApplyNewValue(newValue as TValue);
            EditorGUI.EndProperty();
        }

        private void HandlePickButton(Rect position) {
            var buttonRect = new Rect(position.xMax - PICK_BUTTON_WIDTH, position.y, PICK_BUTTON_WIDTH, position.height);

            var evt = Event.current;
            if (evt.type == EventType.MouseDown
             && evt.button == 0
             && buttonRect.Contains(evt.mousePosition)) {
                evt.Use();

                ObjectSearchWindow.Open(
                    btnRect: buttonRect
                  , title: typeof(TValue).Name
                  , onSelected: node => ApplyNewValue(node.Data as TValue)
                  , filterType: typeof(TValue)
                  , rootPrefab: rootPrefab
                  , allowSceneObjects
                );
            }
        }

        private void ApplyNewValue(TValue newValue) {
            unityValueProp.objectReferenceValue = newValue as Object;
            unityValueProp.serializedObject.ApplyModifiedProperties();
        }

        private bool DrawMismatchBoxIfNeeded() {
            foreach (var weakValue in ValueEntry.WeakValues) {
                if (weakValue is not SerObject<TValue> serObj) continue;
                if (serObj.value == null) continue;   // empty or destroyed, which is the required box's job
                if (serObj.value is TValue) continue;

                SirenixEditorGUI.MessageBox(
                    $"{serObj.value.GetType().Name} is not a {typeof(TValue).Name}. Left over from an older type, and it reads as null at runtime."
                  , MessageType.Error);

                return true;
            }

            return false;
        }

        private void DrawRequiredBoxIfNeeded() {
            if (requiredAttr == null) return;
            if (!HasMissingRequiredValue()) return;

            var message = string.IsNullOrEmpty(requiredAttr.ErrorMessage)
                ? $"{Property.NiceName} is required."
                : requiredAttr.ErrorMessage;

            SirenixEditorGUI.MessageBox(message, (MessageType)requiredAttr.MessageType);
        }

        private bool HasMissingRequiredValue() {
            // Note: must go through IsNull (Unity's == overload). A pattern like { value: null }
            // compares by reference and misses Unity's fake-null objects.
            foreach (var value in ValueEntry.WeakValues) {
                if (value is SerObject<TValue> { IsNull: true }) return true;
            }

            return false;
        }

        public void Dispose() {
            inlineEditorImitator?.Dispose();
        }
    }
}
