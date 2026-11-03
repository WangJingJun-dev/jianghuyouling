using Game.Views.SystemSetting;
using HarmonyLib;
using UnityEngine;

namespace JianghuYouling
{
    [HarmonyPatch(typeof(SettingGroupItem), "UpdateItemInteractableState")]
    internal static class AISettingsMediaInteractablePatch
    {
        private static void Postfix(SettingItemBase item, ISettingItemInfo info)
        {
            bool voice = info.Attribute.SubCategory == AISettingsCategorySync.VoiceSubCategory;
            bool image = info.Attribute.SubCategory == AISettingsCategorySync.ImageSubCategory;
            if (!voice && !image) return;
            if (info.Attribute.LanguageKey == AISettingsVoicePane.EnabledTitle
                || info.Attribute.LanguageKey == AISettingsImagePane.EnabledTitle) return;
            bool enabled = voice ? TtsConfig.IsEnabled : ImageGenerationConfig.LoadRaw().Enabled;
            item.SetInteractable(enabled);
            CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>()
                ?? item.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = enabled ? 1f : 0.5f;
        }
    }
}
