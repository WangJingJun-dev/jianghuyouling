using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Views.SystemSetting;

namespace JianghuYouling
{
    internal static class AISettingsMonthlyPane
    {
        internal const LanguageKey AiEventTitle = (LanguageKey)6150;
        internal const LanguageKey CompanionTitle = (LanguageKey)6151;
        internal const LanguageKey PopupTitle = (LanguageKey)6152;
        internal const LanguageKey CountTitle = (LanguageKey)6153;
        internal const LanguageKey CandidatesTitle = (LanguageKey)6154;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            ESettingSubCategory sub = AISettingsCategorySync.MonthlySubCategory;
            var infos = new List<ISettingItemInfo>
            {
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 1, AiEventTitle),
                    AiEventStore.Load, value => AISettingsSave.Checked(AiEventStore.Save(value), "ai_event")),
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 2, CompanionTitle),
                    CompanionMonthlyStore.Load,
                    value => AISettingsSave.Checked(CompanionMonthlyStore.Save(value), "companion_monthly")),
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 3, PopupTitle),
                    MonthlyDigestPopupStore.Load, value =>
                    {
                        if (AISettingsSave.Checked(MonthlyDigestPopupStore.Save(value), "monthly_popup"))
                            MonthlyDigestPopup.NotifySettingsChanged();
                    }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 4, CountTitle)
                    { CharacterLimit = 10 },
                    () => CompanionMonthlyStore.LoadCount().ToString(CultureInfo.InvariantCulture), value =>
                    {
                        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int count)
                            || count < 0 || count > CompanionMonthlyStore.MaxCount)
                            throw new InvalidOperationException("monthly_count_invalid");
                        if (!CompanionMonthlyStore.SaveCount(count))
                            throw new InvalidOperationException("monthly_count_save_failed");
                    })
            };
            infos.AddRange(AISettingsMonthlyCandidateView.CreateInfos());
            return infos;
        }
    }
}
