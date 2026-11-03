using FrameWork.UISystem.UIElements;
using Game.Views.SystemSetting;
using HarmonyLib;
using UnityEngine;

namespace JianghuYouling
{
    [HarmonyPatch(typeof(SettingGroupItem), "UpdateItemInteractableState")]
    internal static class AISettingsExperienceInteractablePatch
    {
        private static void Postfix(SettingItemBase item, ISettingItemInfo info)
        {
            if (info.Attribute.SubCategory != AISettingsCategorySync.ExperienceSubCategory) return;
            NativeSettingControlFactory.SetRowHoverEnabled(item, false);
        }
    }
}
