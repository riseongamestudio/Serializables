using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Serializables.Editor {
    public class EnumMapDrawer<TEnumMap, TKey, TValue> : OdinValueDrawer<TEnumMap>
        where TEnumMap : EnumMap<TKey, TValue>, new()
        where TKey : struct, Enum {
        private string searchText;

        private InspectorProperty entriesProp;
        private DisplayValueWhenSingleKeyAttribute displayValueWhenSingleKeyAttr;
        private bool needSyncEnumData;

        protected override void Initialize() {
            entriesProp = Property.Children[nameof(EnumMap<TKey, TValue>.entries)];

            displayValueWhenSingleKeyAttr = Property.GetAttribute<DisplayValueWhenSingleKeyAttribute>();

            needSyncEnumData = !ValueEntry.SmartValue.IsUpToDate;
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var map      = ValueEntry.SmartValue;
            var mapLabel = label ?? Property.Label;

            TryDrawSyncAlert();

            if (TryDrawValueWhenSingleKey(map, mapLabel)) return;

            DrawEnumMap(map, mapLabel);
        }

        private void TryDrawSyncAlert() {
            if (!needSyncEnumData) return;

            SirenixEditorGUI.WarningMessageBox("The keys of this enum are not up to date!");

            if (SirenixEditorGUI.SDFIconButton("Update all keys", EditorGUIUtility.singleLineHeight, SdfIconType.ArrowRepeat)) {
                SyncEnumKey();
            }
        }

        private bool TryDrawValueWhenSingleKey(EnumMap<TKey, TValue> map, GUIContent label) {
            if (displayValueWhenSingleKeyAttr == null || map.Count != 1 || entriesProp.Children.Count == 0) return false;

            if (!string.IsNullOrEmpty(displayValueWhenSingleKeyAttr.Label)) label.text = displayValueWhenSingleKeyAttr.Label;
            entriesProp.Children[0].Draw(label);
            return true;
        }

        private void DrawEnumMap(EnumMap<TKey, TValue> map, GUIContent label) {
            SirenixEditorGUI.BeginBox();

            SirenixEditorGUI.BeginToolbarBoxHeader();
            var evt         = Event.current;
            var isAltShift  = evt.alt && evt.shift;
            var curExpanded = Property.State.Expanded;
            var newExpanded = SirenixEditorGUI.Foldout(curExpanded, label);

            if (curExpanded != newExpanded && evt.type == EventType.Used) {
                if (isAltShift) SetAllChildrenExpanded(Property, newExpanded);

                Property.State.Expanded = newExpanded;
            }

            if (newExpanded) {
                searchText = SirenixEditorGUI.ToolbarSearchField(searchText, marginLeftRight: 0);
            }

            SirenixEditorGUI.EndToolbarBoxHeader();

            if (SirenixEditorGUI.BeginFadeGroup(this, newExpanded)) {
                var matchCnt   = 0;
                var hasSearch  = !string.IsNullOrWhiteSpace(searchText);
                var entryCount = Math.Min(entriesProp.Children.Count, map.entries.Count);

                SirenixEditorGUI.BeginVerticalList(false, false);
                for (int i = 0; i < entryCount; i++) {
                    var name = map.entries[i].name ?? string.Empty;
                    if (hasSearch && !name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }

                    SirenixEditorGUI.BeginListItem(false);
                    entriesProp.Children[i].Draw(new GUIContent(name));
                    SirenixEditorGUI.EndListItem();

                    matchCnt++;
                }
                SirenixEditorGUI.EndVerticalList();

                if (matchCnt == 0) {
                    SirenixEditorGUI.InfoMessageBox(hasSearch
                        ? $"No keys match '{searchText}'"
                        : $"{typeof(TKey).Name} is empty");
                }
            }

            SirenixEditorGUI.EndFadeGroup();

            SirenixEditorGUI.EndBox();
        }

        private void SetAllChildrenExpanded(InspectorProperty prop, bool isExpanded) {
            prop.State.Expanded = isExpanded;

            if (prop.Children is not { Count: > 0 }) return;

            foreach (var child in prop.Children) { SetAllChildrenExpanded(child, isExpanded); }
        }

        private void SyncEnumKey() {
            var oldMap = ValueEntry.SmartValue;
            var newMap = new TEnumMap();

            // Reading the old map already matches its saved entries by name, then by number.
            foreach (var key in newMap.Keys) newMap[key] = oldMap[key];

            ValueEntry.SmartValue = newMap;
            ValueEntry.ApplyChanges();

            needSyncEnumData = false;
        }
    }
}
