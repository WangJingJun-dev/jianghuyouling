using System.Reflection;
using Game.Views.EventWindow;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using FrameWork.UISystem.UIElements;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EwChar = Game.Components.EventWindow.EventWindowCharacter;

namespace JianghuYouling
{
    internal enum MiscInteractionAction
    {
        MonthlyCandidate,
        Novel,
        Persona,
        AddMembers,
        Export,
        Clear,
    }

    internal static class MiscInteractionTab
    {
        private const int Index = 6;
        private const string ToggleName = "JHYL_MiscInteractionToggle";
        private const string ContentName = "JHYL_MiscInteractionContent";
        private static readonly FieldInfo ToggleSwitchModeField = typeof(ViewEventWindow).GetField(
            "toggleSwitchMode", BindingFlags.Instance | BindingFlags.NonPublic);
        private static ViewEventWindow _window;
        private static GameObject _content;
        private static bool _show;
        private static bool _nativeHidden;
        private static bool? _operateAreaActive;
        private static bool? _optionScrollActive;

        internal static void TryRefresh(ViewEventWindow w, bool show)
        {
            _show = show;
            if (w == null) { Hide(); return; }
            CToggleGroup group = EwReflect.InteractionGroup(w);
            RectTransform root = EwReflect.MainRoot(w);
            if (group == null || root == null) { Hide(); return; }
            if (_window != w)
            {
                Hide();
                if (_content != null) Object.Destroy(_content);
                _content = null;
                _window = w;
            }
            EnsureToggle(group);
            if (_content == null) _content = CreateContent(w, root);
            if (_show && group.GetActiveIndex() == Index) Show(); else Hide();
        }

        private static void EnsureToggle(CToggleGroup group)
        {
            if (group.transform.Find(ToggleName) != null) return;
            CToggle native = group.Get(0);
            if (native == null) return;
            CToggle miscToggle = Object.Instantiate(native, group.transform, false);
            miscToggle.name = ToggleName;
            miscToggle.isOn = false;
            SetToggleLabel(miscToggle.transform, "杂务");
            PointerTrigger pointer = miscToggle.GetComponent<PointerTrigger>();
            if (pointer != null)
            {
                if (pointer.EnterEvent == null) pointer.EnterEvent = new UnityEvent();
                pointer.EnterEvent.AddListener(() =>
                {
                    Debug.Log("[JHYL_MISC] pointer-enter");
                    SelectOnHover(group);
                });
            }
            else Debug.LogWarning("[JHYL_MISC] cloned toggle has no PointerTrigger");
            group.Add(miscToggle);
            RectTransform toggleRect = miscToggle.GetComponent<RectTransform>();
            Debug.Log("[JHYL_MISC] toggle-created count=" + group.Count()
                + " active=" + miscToggle.isActiveAndEnabled
                + " interactable=" + miscToggle.interactable
                + " pos=" + toggleRect.anchoredPosition + " size=" + toggleRect.sizeDelta);
            group.OnActiveIndexChange += (newIndex, oldIndex) =>
            {
                Debug.Log("[JHYL_MISC] group-index new=" + newIndex + " old=" + oldIndex);
                if (newIndex == Index) Show();
                else Hide();
            };
        }

        private static void SelectOnHover(CToggleGroup group)
        {
            CToggle mode = ToggleSwitchModeField?.GetValue(_window) as CToggle;
            CToggle toggle = group.Get(Index);
            Debug.Log("[JHYL_MISC] hover-select show=" + _show + " mode=" + (mode != null && mode.isOn)
                + " toggle=" + (toggle != null && toggle.interactable));
            if (mode != null && !mode.isOn) return;
            if (_show && toggle != null && toggle.interactable) group.Set(Index);
        }

        private static void SetToggleLabel(Transform toggle, string text)
        {
            TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(toggle, false);
                label = go.GetComponent<TextMeshProUGUI>();
                label.raycastTarget = false;
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 18f;
            }
            label.text = text;
        }

        private static GameObject CreateContent(ViewEventWindow w, RectTransform root)
        {
            RectTransform nativeArea = EwReflect.OptionContainer(w);
            RectTransform optionContainer = EwReflect.OptionContainer(w);
            RectTransform nativeFrame = EwReflect.OptionScroll(w)?.transform as RectTransform;
            EventWindowOption[] nativeOptions = optionContainer != null
                ? optionContainer.GetComponentsInTopChildren<EventWindowOption>(true) : null;
            if (nativeArea == null || nativeFrame == null || nativeFrame.parent == null
                || nativeOptions == null || nativeOptions.Length == 0) return null;
            var content = new GameObject(ContentName, typeof(RectTransform), typeof(Image));
            content.transform.SetParent(nativeFrame.parent, false);
            RectTransform rt = content.GetComponent<RectTransform>();
            rt.anchorMin = nativeFrame.anchorMin;
            rt.anchorMax = nativeFrame.anchorMax;
            rt.pivot = nativeFrame.pivot;
            rt.offsetMin = nativeFrame.offsetMin;
            rt.offsetMax = nativeFrame.offsetMax;
            rt.localScale = nativeFrame.localScale;
            rt.localRotation = nativeFrame.localRotation;
            content.GetComponent<Image>().color = Color.clear;
            VerticalLayoutGroup nativeLayout = optionContainer.GetComponent<VerticalLayoutGroup>();
            if (nativeLayout != null)
            {
                VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(nativeLayout.padding.left, nativeLayout.padding.right,
                    nativeLayout.padding.top, nativeLayout.padding.bottom);
                layout.spacing = nativeLayout.spacing;
                layout.childAlignment = nativeLayout.childAlignment;
                layout.childControlWidth = nativeLayout.childControlWidth;
                layout.childControlHeight = nativeLayout.childControlHeight;
                layout.childForceExpandWidth = nativeLayout.childForceExpandWidth;
                layout.childForceExpandHeight = nativeLayout.childForceExpandHeight;
                layout.childScaleWidth = nativeLayout.childScaleWidth;
                layout.childScaleHeight = nativeLayout.childScaleHeight;
            }
            string[] labels = { "加入主动", "往事成书", "人设", "加人", "导出", "清空" };
            string[] helpTexts =
            {
                "将当前人物加入或移出主动人物名单",
                "以当前人物的聊天与记忆撰写往事",
                "编辑当前人物的自定义人设",
                "选择人物加入当前群聊",
                "导出当前人物的聊天记录",
                "清空当前人物的聊天记录，需要再次确认",
            };
            MiscInteractionAction[] actions =
            {
                MiscInteractionAction.MonthlyCandidate,
                MiscInteractionAction.Novel,
                MiscInteractionAction.Persona,
                MiscInteractionAction.AddMembers,
                MiscInteractionAction.Export,
                MiscInteractionAction.Clear,
            };
            for (int i = 0; i < labels.Length; i++)
            {
                MiscInteractionAction action = actions[i];
                GameObject row = Object.Instantiate(nativeOptions[0].gameObject, content.transform, false);
                row.name = "Misc_" + action;
                row.SetActive(true);
                EventWindowOption option = row.GetComponent<EventWindowOption>();
                var info = new EventOptionInfo
                {
                    OptionKey = row.name,
                    OptionContent = "（" + labels[i] + "……）",
                    OptionState = 0,
                };
                option.Setup(info, (i + 1).ToString(), 0, 0, 0, _ => Invoke(w, action));
                EwReflect.SetOptionHelp(option, helpTexts[i]);
            }
            content.SetActive(false);
            LogLayout(content, "created");
            return content;
        }

        private static void LogLayout(GameObject content, string stage)
        {
            if (content == null) return;
            RectTransform rt = content.GetComponent<RectTransform>();
            string parents = string.Empty;
            for (Transform parent = content.transform.parent; parent != null; parent = parent.parent)
                parents = parent.name + "(" + parent.gameObject.activeSelf + ")/" + parents;
            Debug.Log("[JHYL_MISC] layout " + stage + " parents=" + parents
                + " min=" + rt.offsetMin + " max=" + rt.offsetMax + " size=" + rt.sizeDelta
                + " pos=" + rt.anchoredPosition + " children=" + content.transform.childCount);
            if (content.transform.childCount == 0) return;
            RectTransform row = content.transform.GetChild(0) as RectTransform;
            if (row == null) return;
            TextMeshProUGUI label = FindLabel(row);
            Debug.Log("[JHYL_MISC] first-row active=" + row.gameObject.activeSelf
                + " pos=" + row.anchoredPosition + " size=" + row.sizeDelta
                + " label=" + (label == null ? "null" : label.text)
                + " font=" + (label == null || label.font == null ? "null" : label.font.name)
                + " enabled=" + (label != null && label.enabled));
        }

        private static TextMeshProUGUI FindLabel(Transform root)
        {
            TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] != null && (labels[i].name == "OptionName" || labels[i].name == "Label"))
                    return labels[i];
            return labels.Length > 0 ? labels[0] : null;
        }

        private static void Invoke(ViewEventWindow w, MiscInteractionAction action)
        {
            EwChar character = EwReflect.Right(w);
            int npcId = EwReflect.ResolveNpcId(character);
            if (!_show || !EwReflect.IsCurrentTarget(w, character, npcId)) return;
            int taiwuId = EwReflect.TaiwuId();
            string npcName = character != null && character.nameLabel != null
                ? character.nameLabel.text : "NPC#" + npcId;
            TMP_FontAsset font = character != null && character.nameLabel != null
                ? character.nameLabel.font : null;
            ChatWindow.InvokeMiscActionForNpc(npcId, taiwuId, npcName, font, action);
        }

        internal static void Show()
        {
            if (!_show) return;
            bool firstShow = _content != null && !_content.activeSelf;
            if (!_nativeHidden && _window != null)
            {
                RectTransform operateArea = EwReflect.OperateArea(_window);
                CScrollRect optionScroll = EwReflect.OptionScroll(_window);
                _operateAreaActive = operateArea != null && operateArea.gameObject.activeSelf;
                _optionScrollActive = optionScroll != null && optionScroll.gameObject.activeSelf;
                if (operateArea != null) operateArea.gameObject.SetActive(false);
                _nativeHidden = true;
            }
            CScrollRect currentOptionScroll = _window != null ? EwReflect.OptionScroll(_window) : null;
            if (currentOptionScroll != null && currentOptionScroll.gameObject.activeSelf)
                currentOptionScroll.gameObject.SetActive(false);
            if (_content != null) _content.SetActive(true);
            if (firstShow)
            {
                Debug.Log("[JHYL_MISC] content-shown content=" + (_content != null)
                    + " nativeHidden=" + _nativeHidden);
                LogLayout(_content, "shown");
            }
        }

        internal static void Hide()
        {
            if (_content != null) _content.SetActive(false);
            RestoreNativeContent();
        }

        private static void RestoreNativeContent()
        {
            if (!_nativeHidden) return;
            RectTransform operateArea = EwReflect.OperateArea(_window);
            CScrollRect optionScroll = EwReflect.OptionScroll(_window);
            if (operateArea != null && _operateAreaActive.HasValue)
                operateArea.gameObject.SetActive(_operateAreaActive.Value);
            if (optionScroll != null && _optionScrollActive.HasValue)
                optionScroll.gameObject.SetActive(_optionScrollActive.Value);
            _nativeHidden = false;
            _operateAreaActive = null;
            _optionScrollActive = null;
        }

        internal static void Reset()
        {
            _show = false;
            Hide();
            if (_content != null) Object.Destroy(_content);
            _content = null;
            _window = null;
        }
    }

    [HarmonyPatch(typeof(ViewEventWindow), "OnActiveToggleChange")]
    internal static class MiscInteractionSelectionPatch
    {
        private const int Index = 6;

        private static bool Prefix(int newToggleIndex, int oldToggleIndex)
        {
            if (newToggleIndex != Index) return true;
            Debug.Log("[JHYL_MISC] native-selection intercepted old=" + oldToggleIndex);
            MiscInteractionTab.Show();
            return false;
        }
    }

    [HarmonyPatch(typeof(ViewEventWindow), "SyncCommonOptionToggleVisuals")]
    internal static class MiscInteractionVisualSyncPatch
    {
        private const int Index = 6;

        private static bool Prefix(ViewEventWindow __instance)
        {
            bool native = EwReflect.InteractionGroup(__instance)?.GetActiveIndex() != Index;
            if (!native) Debug.Log("[JHYL_MISC] native-visual-sync skipped");
            return native;
        }
    }

    [HarmonyPatch(typeof(CToggle), "OnPointerClick")]
    internal static class MiscInteractionPointerPatch
    {
        private static void Prefix(CToggle __instance, PointerEventData eventData)
        {
            if (__instance.name != "JHYL_MiscInteractionToggle") return;
            Debug.Log("[JHYL_MISC] pointer-click button=" + eventData.button
                + " active=" + __instance.isActiveAndEnabled
                + " interactable=" + __instance.interactable + " isOn=" + __instance.isOn);
        }
    }
}
