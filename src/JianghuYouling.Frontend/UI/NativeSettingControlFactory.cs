using System.Collections.Generic;
using System.Reflection;
using FrameWork.UISystem.UIElements;
using Game.Views.SystemSetting;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal static class NativeSettingControlFactory
    {
        internal static void Register(Dictionary<SettingUIType, SettingItemBase> prefabMap, ViewSystemSetting window)
        {
            if (prefabMap == null || window == null) return;
            if (prefabMap.ContainsKey((SettingUIType)100) && prefabMap.ContainsKey((SettingUIType)101)
                && prefabMap.ContainsKey((SettingUIType)102) && prefabMap.ContainsKey((SettingUIType)103)
                && prefabMap.ContainsKey((SettingUIType)104) && prefabMap.ContainsKey((SettingUIType)105)) return;
            FieldInfo field = typeof(ViewSystemSetting).GetField("switchButtonSettingPrefab",
                BindingFlags.Instance | BindingFlags.NonPublic);
            SettingItemBase template = field?.GetValue(window) as SettingItemBase;
            if (template == null)
            {
                Debug.LogError("[JHYL_AI_SETTINGS] native control template missing");
                return;
            }
            prefabMap[(SettingUIType)100] = CreatePrefab<NativeTextSettingItem>(template, "JHYL_TextSettingPrefab", window);
            prefabMap[(SettingUIType)101] = CreatePrefab<NativeActionSettingItem>(template, "JHYL_ActionSettingPrefab", window);
            prefabMap[(SettingUIType)102] = CreatePrefab<NativeImageSettingItem>(template, "JHYL_ImageSettingPrefab", window);
            prefabMap[(SettingUIType)103] = CreatePrefab<NativeExperiencePresetSettingItem>(template, "JHYL_PresetCardSettingPrefab", window);
            prefabMap[(SettingUIType)104] = CreatePrefab<NativeReadonlyTextSettingItem>(template, "JHYL_ReadonlyTextSettingPrefab", window);
            prefabMap[(SettingUIType)105] = CreatePrefab<NativeImageSizeSettingItem>(template, "JHYL_ImageSizeSettingPrefab", window);
            Debug.Log("[JHYL_AI_SETTINGS] native text/action/image/readonly prefabs registered");
        }

        private static T CreatePrefab<T>(SettingItemBase template, string name, ViewSystemSetting window) where T : SettingItemBase
        {
            GameObject go = Object.Instantiate(template.gameObject);
            go.name = name;
            go.SetActive(false);
            SettingItemBase original = go.GetComponent<SettingItemBase>();
            TextMeshProUGUI label = typeof(SettingItemBase).GetField("labelText",
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(original) as TextMeshProUGUI;
            TMP_FontAsset font = null;
            Material sharedMaterial = null;
            if (original is SwitchButtonSettingItem switchItem)
            {
                TextMeshProUGUI valueText = typeof(SwitchButtonSettingItem).GetField("valueText",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(switchItem) as TextMeshProUGUI;
                if (valueText != null)
                {
                    font = valueText.font;
                    sharedMaterial = valueText.fontSharedMaterial;
                }
            }
            if (font == null && label != null) font = label.font;
            if (sharedMaterial == null && label != null) sharedMaterial = label.fontSharedMaterial;
            if (ConfigHost.Instance != null) font = ConfigHost.Instance.ResolveFont();
            if (sharedMaterial == null && font != null) sharedMaterial = font.material;
            foreach (Transform child in go.transform)
            {
                if (label != null && (child == label.transform || label.transform.IsChildOf(child))) continue;
                child.gameObject.SetActive(false);
            }
            if (original != null) Object.DestroyImmediate(original);
            T item = go.AddComponent<T>();
            if (item is NativeTextSettingItem textItem) textItem.Configure(label, font, sharedMaterial);
            if (item is NativeActionSettingItem actionItem) actionItem.Configure(label, font, sharedMaterial);
            if (item is NativeImageSettingItem imageItem) imageItem.Configure(label, font, sharedMaterial);
            if (item is NativeExperiencePresetSettingItem presetItem) presetItem.Configure(label);
            if (item is NativeReadonlyTextSettingItem readonlyItem) readonlyItem.Configure(label, font, sharedMaterial);
            if (item is NativeImageSizeSettingItem sizeItem) sizeItem.Configure(label, font, sharedMaterial);
            return item;
        }

        internal static void SetRowHoverEnabled(SettingItemBase item, bool enabled)
        {
            if (item == null) return;
            Transform row = item.transform.parent;
            if (row == null) return;
            PointerTrigger pointerTrigger = row.GetComponent<PointerTrigger>();
            if (pointerTrigger != null)
            {
                pointerTrigger.enabled = enabled;
                if (!enabled)
                {
                    pointerTrigger.EnterEvent.RemoveAllListeners();
                    pointerTrigger.ExitEvent.RemoveAllListeners();
                }
            }
            CImage frame = row.GetComponent<CImage>();
            if (frame == null) return;
            frame.enabled = enabled;
            if (!enabled) frame.SetAlpha(0f);
        }

        internal static void RefreshNavigation(SettingGroupItem group)
        {
            if (group == null) return;
            SettingItemBase[] items = group.GetComponentsInChildren<SettingItemBase>(true);
            var rows = new System.Collections.Generic.List<Selectable[]>();
            foreach (SettingItemBase item in items)
            {
                if (item is NativeTextSettingItem text) rows.Add(text.Focusables());
                else if (item is NativeActionSettingItem action) rows.Add(action.Focusables());
                else if (item is NativeExperiencePresetSettingItem preset) rows.Add(preset.Focusables());
                else if (item is NativeImageSizeSettingItem size) rows.Add(size.Focusables());
                else rows.Add(item.GetComponentsInChildren<Selectable>(true));
            }
            for (int row = 0; row < items.Length; row++)
            {
                bool custom = items[row] is NativeTextSettingItem || items[row] is NativeActionSettingItem
                    || items[row] is NativeExperiencePresetSettingItem || items[row] is NativeImageSizeSettingItem;
                if (!custom) continue;
                for (int column = 0; column < rows[row].Length; column++)
                {
                    Selectable current = rows[row][column];
                    if (current == null) continue;
                    Selectable previous = FindSelectable(items, rows, row, column, -1);
                    Selectable next = FindSelectable(items, rows, row, column, 1);
                    Navigation navigation = current.navigation;
                    navigation.mode = Navigation.Mode.Explicit;
                    navigation.selectOnUp = previous;
                    navigation.selectOnDown = next;
                    navigation.selectOnLeft = previous;
                    navigation.selectOnRight = next;
                    current.navigation = navigation;
                }
            }
            AISettingsStatusView.Refresh(group);
        }

        private static Selectable FindSelectable(SettingItemBase[] items,
            System.Collections.Generic.List<Selectable[]> rows, int row, int column, int direction)
        {
            int currentRow = row;
            int currentColumn = column + direction;
            while (currentRow >= 0 && currentRow < items.Length)
            {
                Selectable[] candidates = rows[currentRow];
                if (currentColumn >= 0 && currentColumn < candidates.Length)
                    return candidates[currentColumn];
                currentRow += direction;
                if (currentRow < 0 || currentRow >= items.Length) break;
                currentColumn = direction < 0 ? rows[currentRow].Length - 1 : 0;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(SettingGroupItem), "Set")]
    internal static class NativeSettingNavigationPatch
    {
        private static void Postfix(SettingGroupItem __instance)
            => NativeSettingControlFactory.RefreshNavigation(__instance);
    }
}
