using System.Collections.Generic;
using FrameWork.UISystem.UIElements;
using Game.Views.SystemSetting;
using HarmonyLib;
using UnityEngine;

namespace JianghuYouling
{
    internal static class AISettingsWorldBookPane
    {
        internal static List<ISettingItemInfo> CreateInfos() => AISettingsWorldBookEditor.CreateInfos();
    }

    [HarmonyPatch(typeof(SettingGroupItem), "UpdateItemInteractableState")]
    internal static class AISettingsWorldBookInteractablePatch
    {
        private static void Postfix(SettingItemBase item, ISettingItemInfo info)
        {
            bool worldBook = info.Attribute.SubCategory == AISettingsCategorySync.WorldBookSubCategory;
            bool monthlyCandidates = info.Attribute.SubCategory == AISettingsCategorySync.MonthlySubCategory
                && (info.Attribute.LanguageKey == AISettingsMonthlyCandidateView.SelectedTitle
                    || info.Attribute.LanguageKey == AISettingsMonthlyCandidateView.ToggleTitle);
            if (!worldBook && !monthlyCandidates) return;
            bool available = worldBook
                ? AISettingsWorldBookEditor.IsAvailable
                : AISettingsMonthlyCandidateView.IsAvailable;
            item.SetInteractable(available);
            CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>() ?? item.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = available ? 1f : 0.5f;
            Transform row = item.transform.parent;
            if (row == null) return;
            PointerTrigger pointerTrigger = row.GetComponent<PointerTrigger>();
            if (pointerTrigger == null) return;
            pointerTrigger.enabled = available;
            if (!available)
            {
                pointerTrigger.EnterEvent.RemoveAllListeners();
                pointerTrigger.ExitEvent.RemoveAllListeners();
            }
            CImage frame = row.GetComponent<CImage>();
            if (frame == null) return;
            frame.enabled = available;
            if (!available) frame.SetAlpha(0f);
        }
    }
}
