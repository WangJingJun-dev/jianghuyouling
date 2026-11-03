using System.Collections.Generic;
using Game.Views.SystemSetting;
using HarmonyLib;
using JianghuYouling.Core.Text;
using UnityEngine;

namespace JianghuYouling
{
    internal static class AISettingsAboutPane
    {
        internal const LanguageKey OpenSourceTitle = (LanguageKey)6350;
        internal const LanguageKey OpenLinkTitle = (LanguageKey)6351;
        internal const LanguageKey CopyLinkTitle = (LanguageKey)6352;
        internal const LanguageKey SupportTitle = (LanguageKey)6353;
        internal const LanguageKey QrTitle = (LanguageKey)6354;
        internal const LanguageKey OpenLinkActionText = (LanguageKey)6355;
        internal const LanguageKey CopyLinkActionText = (LanguageKey)6356;
        internal const LanguageKey QrCaption = (LanguageKey)6357;
        internal const LanguageKey QrFallbackCaption = (LanguageKey)6358;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            ESettingSubCategory sub = AISettingsCategorySync.AboutSubCategory;
            Sprite qr;
            ModIntroductionWindow.TryLoadSupportQr(out qr);
            return new List<ISettingItemInfo>
            {
                new SettingItemInfo<string>(new NativeReadonlyTextSettingAttribute(sub, 1, OpenSourceTitle)
                    { CenterVertically = true }, () => OpenSourceProjectContent.FullText, _ => { }),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 2, OpenLinkTitle)
                    {
                        ActionText = OpenLinkActionText,
                        SecondaryActionText = CopyLinkActionText,
                        SecondaryAction = CopyProjectLink
                    }, () => 0, _ => OpenProject()),
                new SettingItemInfo<string>(new NativeReadonlyTextSettingAttribute(sub, 4, SupportTitle)
                    { CenterVertically = true }, SupportText, _ => { }),
                new SettingItemInfo<sbyte>(new NativeImageSettingAttribute(sub, 5, QrTitle)
                    { Image = qr, Caption = qr != null ? QrCaption : QrFallbackCaption }, () => 0, _ => { })
            };
        }

        private static string SupportText()
        {
            return SupportDevelopmentContent.SupportText + "\n\n" + SupportDevelopmentContent.OverviewText
                + "\n\n【鸣谢】\n" + SupportDevelopmentContent.AcknowledgementNames + "\n"
                + SupportDevelopmentContent.AcknowledgementNote;
        }

        private static void OpenProject() => Application.OpenURL(OpenSourceProjectContent.Url);

        private static void CopyProjectLink() => GUIUtility.systemCopyBuffer = OpenSourceProjectContent.Url;
    }

    [HarmonyPatch(typeof(SettingGroupItem), "UpdateItemInteractableState")]
    internal static class AISettingsAboutHoverPatch
    {
        private static void Postfix(SettingItemBase item, ISettingItemInfo info)
        {
            if (info.Attribute.SubCategory != AISettingsCategorySync.AboutSubCategory) return;
            NativeSettingControlFactory.SetRowHoverEnabled(item, false);
        }
    }
}
