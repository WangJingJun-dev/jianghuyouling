using System;
using System.Collections.Generic;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Persona;

namespace JianghuYouling
{
    internal static class AISettingsAssistantPane
    {
        internal const LanguageKey EnabledTitle = (LanguageKey)6101;
        internal const LanguageKey ProactiveTitle = (LanguageKey)6102;
        internal const LanguageKey CommissionTitle = (LanguageKey)6103;
        internal const LanguageKey OffText = (LanguageKey)6104;
        internal const LanguageKey LowText = (LanguageKey)6105;
        internal const LanguageKey MidText = (LanguageKey)6106;
        internal const LanguageKey HighText = (LanguageKey)6107;
        internal const LanguageKey NameTitle = (LanguageKey)6117;
        internal const LanguageKey FaceTitle = (LanguageKey)6118;
        internal const LanguageKey PersonaTitle = (LanguageKey)6119;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            ESettingSubCategory sub = AISettingsCategorySync.AssistantSubCategory;
            return new List<ISettingItemInfo>
            {
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 1, EnabledTitle),
                    AssistantEnabledStore.Load, value =>
                    {
                        if (AISettingsSave.Checked(AssistantEnabledStore.Save(value), "assistant_enabled"))
                            AssistantWidget.NotifySettingsChanged();
                    }),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 2, ProactiveTitle)
                    { Options = new[] { OffText, LowText, MidText, HighText } },
                    () => (sbyte)AssistantProactiveStore.Load(), value =>
                    {
                        if (AISettingsSave.Checked(AssistantProactiveStore.Save(value), "assistant_proactive"))
                            AssistantWidget.NotifySettingsChanged();
                    }),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 3, CommissionTitle)
                    { Options = new[] { OffText, LowText, MidText, HighText } },
                    () => (sbyte)AssistantCommissionIntervalStore.Load(), value =>
                    {
                        if (AISettingsSave.Checked(AssistantCommissionIntervalStore.Save(value), "assistant_commission"))
                            AssistantWidget.NotifySettingsChanged();
                    }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 4, NameTitle)
                    { CharacterLimit = 64 },
                    AssistantNameStore.Load, value =>
                    {
                        if (!AssistantNameStore.Save(value)) throw new InvalidOperationException("assistant_name_save_failed");
                        AssistantWidget.NotifySettingsChanged();
                    }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 5, FaceTitle)
                    { CharacterLimit = 4096 },
                    AssistantFacePathStore.Load, value =>
                    {
                        if (!AssistantFacePathStore.Save(value)) throw new InvalidOperationException("assistant_face_save_failed");
                        AssistantWidget.NotifyFacePathChanged();
                        AssistantWidget.NotifySettingsChanged();
                    }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 6, PersonaTitle)
                    { Multiline = true, CharacterLimit = PersonaStore.MaxCustomPersonaChars },
                    AssistantPersonaStore.LoadDisplay, value =>
                    {
                        if (!AssistantPersonaStore.SaveDisplay(value))
                            throw new InvalidOperationException("assistant_persona_save_failed");
                        AssistantWidget.NotifySettingsChanged();
                    })
            };
        }
    }
}
