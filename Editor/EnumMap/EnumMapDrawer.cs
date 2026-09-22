using System;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Serializables.Editor {
    public class EnumMapDrawer<TEnumMap, TKey, TValue> : OdinValueDrawer<TEnumMap> 
        where TEnumMap : EnumMap<TKey, TValue>, new()
        where TKey : struct, Enum {
        private string searchText;

        private InspectorProperty valuesProp;
        private DisplayValueWhenSingleKeyAttribute displayValueWhenSingleKeyAttr;
        private bool needSyncEnumData;

        protected override void Initialize() {
            valuesProp = Property.Children[nameof(EnumMap<TKey, TValue>._Values)];

            displayValueWhenSingleKeyAttr = Property.GetAttribute<DisplayValueWhenSingleKeyAttribute>();

            var entry     = ValueEntry.SmartValue;
            var trueNames = Enum.GetNames(typeof(TKey));
            needSyncEnumData =
                entry.Count != trueNames.Length
             || !entry._KeyNames.SequenceEqual(trueNames);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var entry      = ValueEntry.SmartValue;
            var entryLabel = label ?? Property.Label;

            TryDrawSyncAlert();

            if (TryDrawValueWhenSingleKey(entry, entryLabel)) return;

            DrawEnumMap(entry, entryLabel);
        }

        private void TryDrawSyncAlert() {
            if (!needSyncEnumData) return;

            SirenixEditorGUI.WarningMessageBox("The keys of this enum are not up to date!");

            if (SirenixEditorGUI.SDFIconButton("Update all keys", EditorGUIUtility.singleLineHeight, SdfIconType.ArrowRepeat)) {
                SyncEnumKey();
            }
        }

        private bool TryDrawValueWhenSingleKey(EnumMap<TKey, TValue> entry, GUIContent label) {
            if (displayValueWhenSingleKeyAttr == null || entry.Count != 1) return false;

            if (!string.IsNullOrEmpty(displayValueWhenSingleKeyAttr.Label)) label.text = displayValueWhenSingleKeyAttr.Label;
            valuesProp.Children[0].Children[nameof(EnumMapItem<TValue>.value)].Draw(label);
            return true;
        }

        private void DrawEnumMap(EnumMap<TKey, TValue> entry, GUIContent label) {
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
                var matchCnt  = 0;
                var hasSearch = !string.IsNullOrWhiteSpace(searchText);

                SirenixEditorGUI.BeginVerticalList(false, false);
                for (int i = 0; i < entry.Count; i++) {
                    if (hasSearch && !entry._KeyNames[i].Contains(searchText, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }

                    SirenixEditorGUI.BeginListItem(false);
                    valuesProp.Children[i].Draw(new GUIContent(entry._KeyNames[i]));
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
            var oldEntry = ValueEntry.SmartValue;
            var newEntry = new TEnumMap();

            for (int i = 0; i < newEntry.Count; ++i) {
                for (int j = 0; j < oldEntry.Count; ++j) {
                    if (newEntry._KeyNames[i] != oldEntry._KeyNames[j]) continue;

                    newEntry._Values[i]   = oldEntry._Values[j];
                    oldEntry._KeyNames[j] = null;
                }
            }

            for (int i = 0; i < newEntry.Count; ++i) {
                for (int j = 0; j < oldEntry.Count; ++j) {
                    if (oldEntry._KeyNames[j] == null) continue;
                    if (!UnsafeUtility.EnumEquals(newEntry._Keys[i], oldEntry._Keys[j])) continue;

                    newEntry._Values[i] = oldEntry._Values[j];
                }
            }

            ValueEntry.SmartValue = newEntry;
            ValueEntry.ApplyChanges();

            needSyncEnumData = false;
        }
    }
}