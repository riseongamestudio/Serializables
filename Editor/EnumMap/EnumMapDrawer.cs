using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using RiseOn.Utils.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Serializables.Editor {
    public class EnumMapDrawer<TEnumMap, TKey, TValue> : OdinValueDrawer<TEnumMap>
        where TEnumMap : EnumMap<TKey, TValue>, new()
        where TKey : struct, Enum {
        private readonly SectionRows rows  = new();
        private readonly List<int>    shown = new();

        private string searchText;

        private InspectorProperty entriesProp;
        private bool needSyncEnumData;

        protected override void Initialize() {
            entriesProp = Property.Children[nameof(EnumMap<TKey, TValue>.entries)];

            needSyncEnumData = !ValueEntry.SmartValue.IsUpToDate;
            Section.Restore(Property);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var map = ValueEntry.SmartValue;

            TryDrawSyncAlert();

            // Odin passes no label under HideLabel: the owner titles and frames the map, so only the rows draw.
            if (label == null) {
                DrawRows(map);
                return;
            }

            // A settings page's section, the keys its rows; the search, in the bar's value column, while it is open.
            using var section = Section.Foldout(Property, label.text, label.tooltip, padded: false, barValue: true);
            if (Property.State.Expanded) searchText = EditorGUI.TextField(section.BarValue, searchText, EditorStyles.toolbarSearchField);
            if (!section.Open) return;

            DrawRows(map);
        }

        private void DrawRows(EnumMap<TKey, TValue> map) {
            var entryCount = Math.Min(entriesProp.Children.Count, map.entries.Count);
            var hasSearch  = !string.IsNullOrWhiteSpace(searchText);
            shown.Clear();
            for (var i = 0; i < entryCount; i++) {
                if (!hasSearch || KeyLabel(map.entries[i].name).Contains(searchText, StringComparison.OrdinalIgnoreCase)) shown.Add(i);
            }

            rows.Draw(shown.Count, i => entriesProp.Children[shown[i]].Draw(new GUIContent(KeyLabel(map.entries[shown[i]].name))),
                hasSearch ? $"No keys match '{searchText}'" : $"{typeof(TKey).Name} is empty");
        }

        // The key as the enum names it in the Inspector: its InspectorName, else the readable member name.
        private static string KeyLabel(string member) {
            if (string.IsNullOrEmpty(member)) return string.Empty;

            var named = typeof(TKey).GetField(member)?.GetCustomAttribute<InspectorNameAttribute>();
            return named != null ? named.displayName : ObjectNames.NicifyVariableName(member);
        }

        private void TryDrawSyncAlert() {
            if (!needSyncEnumData) return;

            if (HelpBoxUtils.Draw($"The keys no longer match {typeof(TKey).Name}: it gained, lost or renamed a member since the map was saved.", MessageType.Warning, new GUIContent("Update keys", "Takes the keys from the enum, keeping each value whose key is still there"))) SyncEnumKey();
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