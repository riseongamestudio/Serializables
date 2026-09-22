using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Serializables.Editor {
    /// <summary>
    /// Folded: the time in UTC+0, as text that can be selected and copied. Unfolded: one slider per part.
    /// Right-click for Now and Reset; Odin adds its own Copy and Paste, which move the value between fields.
    /// </summary>
    public class SerMomentDrawer : OdinValueDrawer<SerMoment>, IDefinesGenericMenuItems {
        private const string HeaderFormat = "HH:mm:ss dd/MM/yyyy";
        private const float  FoldoutWidth = 14f;

        private static readonly GUIContent hourLabel        = new("Hour");
        private static readonly GUIContent minuteLabel      = new("Minute");
        private static readonly GUIContent secondLabel      = new("Second");
        private static readonly GUIContent millisecondLabel = new("Millisecond");
        private static readonly GUIContent dayLabel         = new("Day");
        private static readonly GUIContent monthLabel       = new("Month");
        private static readonly GUIContent yearLabel        = new("Year");

        // The Unity property behind the field, so the prefab override bar, the bold label and Revert keep working.
        private SerializedProperty unityProperty;

        protected override void Initialize() {
            var serializedObject = Property.Tree.UnitySerializedObject;
            var propertyPath     = Property.UnityPropertyPath;
            if (serializedObject != null && !string.IsNullOrEmpty(propertyPath)) {
                unityProperty = serializedObject.FindProperty(propertyPath);
            }
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var moment   = ValueEntry.SmartValue;
            var isValid  = IsValid(moment);
            var hasMixed = HasMixedValues();

            // The whole block, header plus sliders, so the prefab override bar covers all of it.
            var blockRect  = EditorGUILayout.BeginVertical();
            var fieldLabel = label ?? GUIContent.none;
            if (unityProperty != null) fieldLabel = EditorGUI.BeginProperty(blockRect, fieldLabel, unityProperty);

            var rowRect     = EditorGUILayout.GetControlRect();
            var labelWidth  = label == null ? FoldoutWidth : EditorGUIUtility.labelWidth;
            var foldoutRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);
            var valueRect   = new Rect(rowRect.x + labelWidth, rowRect.y, rowRect.width - labelWidth, rowRect.height);

            Property.State.Expanded = SirenixEditorGUI.Foldout(foldoutRect, Property.State.Expanded, fieldLabel);

            var headerText = hasMixed ? "—" : isValid ? moment.ToString(HeaderFormat) : $"Invalid ({moment.UnixMs})";
            EditorGUI.SelectableLabel(valueRect, headerText, EditorStyles.label);

            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded)) {
                EditorGUI.indentLevel++;
                DrawSliders(isValid ? moment : SerMoment.UnixEpoch, hasMixed);
                EditorGUI.indentLevel--;
            }

            SirenixEditorGUI.EndFadeGroup();

            if (unityProperty != null) EditorGUI.EndProperty();
            EditorGUILayout.EndVertical();
        }

        /// <summary>Time first, then date, in the same order as the header.</summary>
        private void DrawSliders(SerMoment moment, bool hasMixed) {
            var utc = moment.Utc;

            EditorGUI.showMixedValue = hasMixed;
            EditorGUI.BeginChangeCheck();

            var hour        = EditorGUILayout.IntSlider(hourLabel, utc.Hour, 0, 23);
            var minute      = EditorGUILayout.IntSlider(minuteLabel, utc.Minute, 0, 59);
            var second      = EditorGUILayout.IntSlider(secondLabel, utc.Second, 0, 59);
            var millisecond = EditorGUILayout.IntSlider(millisecondLabel, utc.Millisecond, 0, 999);

            var day   = EditorGUILayout.IntSlider(dayLabel, utc.Day, 1, DateTime.DaysInMonth(utc.Year, utc.Month));
            var month = EditorGUILayout.IntSlider(monthLabel, utc.Month, 1, 12);
            var year  = EditorGUILayout.IntSlider(yearLabel, utc.Year, 1, 9999);

            var changed = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;

            if (changed) ValueEntry.SmartValue = Compose(year, month, day, hour, minute, second, millisecond);
        }

        /// <summary>Out-of-range parts are clamped, so day 31 in a 30-day month becomes day 30.</summary>
        private static SerMoment Compose(int year, int month, int day, int hour, int minute, int second, int millisecond) {
            year  = Mathf.Clamp(year, 1, 9999);
            month = Mathf.Clamp(month, 1, 12);

            return new SerMoment(new DateTime(
                year,
                month,
                Mathf.Clamp(day, 1, DateTime.DaysInMonth(year, month)),
                Mathf.Clamp(hour, 0, 23),
                Mathf.Clamp(minute, 0, 59),
                Mathf.Clamp(second, 0, 59),
                Mathf.Clamp(millisecond, 0, 999),
                DateTimeKind.Utc));
        }

        private static bool IsValid(SerMoment moment) {
            return moment >= SerMoment.MinValue && moment <= SerMoment.MaxValue;
        }

        private bool HasMixedValues() {
            var values = ValueEntry.Values;
            for (var i = 1; i < values.Count; i++) {
                if (values[i] != values[0]) return true;
            }

            return false;
        }

        void IDefinesGenericMenuItems.PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu) {
            // Odin's own Copy and Paste come first, and they move the value between fields.
            if (genericMenu.GetItemCount() > 0) genericMenu.AddSeparator(string.Empty);

            genericMenu.AddItem(new GUIContent("Now"), false, () => SetValue(SerMoment.Now));
            genericMenu.AddItem(new GUIContent("Reset"), false, () => SetValue(SerMoment.UnixEpoch));
        }

        private void SetValue(SerMoment moment) {
            Property.Tree.DelayActionUntilRepaint(() => {
                for (var i = 0; i < ValueEntry.ValueCount; i++) ValueEntry.Values[i] = moment;
            });
        }
    }
}
