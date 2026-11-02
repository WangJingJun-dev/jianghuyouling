using System;
using System.Collections.Generic;
using Game.Views.SystemSetting;

namespace JianghuYouling
{
    internal static class AISettingsCategorySync
    {
        internal const ESettingCategory AiCategory = (ESettingCategory)100;
        internal const ESettingSubCategory AssistantSubCategory = (ESettingSubCategory)1000;
        internal const ESettingSubCategory ModelSubCategory = (ESettingSubCategory)1001;
        internal const ESettingSubCategory ExperienceSubCategory = (ESettingSubCategory)1002;
        internal const ESettingSubCategory VoiceSubCategory = (ESettingSubCategory)1003;
        internal const ESettingSubCategory ImageSubCategory = (ESettingSubCategory)1004;
        internal const ESettingSubCategory ConversationSubCategory = (ESettingSubCategory)1005;
        internal const ESettingSubCategory WorldBookSubCategory = (ESettingSubCategory)1006;
        internal const ESettingSubCategory MonthlySubCategory = (ESettingSubCategory)1007;
        internal const ESettingSubCategory AboutSubCategory = (ESettingSubCategory)1008;

        internal const LanguageKey AssistantTitle = (LanguageKey)6100;
        internal const LanguageKey AboutTitle = (LanguageKey)6400;
        internal const LanguageKey ModelTitle = (LanguageKey)6110;
        internal const LanguageKey ExperienceTitle = (LanguageKey)6111;
        internal const LanguageKey VoiceTitle = (LanguageKey)6112;
        internal const LanguageKey ImageTitle = (LanguageKey)6113;
        internal const LanguageKey ConversationTitle = (LanguageKey)6114;
        internal const LanguageKey WorldBookTitle = (LanguageKey)6115;
        internal const LanguageKey MonthlyTitle = (LanguageKey)6116;

        internal static bool RequiresRecovery(System.Array current)
            => !(current is SettingCategoryData[]);

        internal static SettingCategoryData[] Synchronize(SettingCategoryData[] current, SettingCategoryData[] nativeBaseline)
        {
            SettingCategoryData[] source = current ?? nativeBaseline ?? new SettingCategoryData[0];
            var categories = new List<SettingCategoryData>(source.Length + 1);
            foreach (SettingCategoryData category in source)
            {
                if (category == null || category.Category == AiCategory) continue;
                categories.Add(category);
            }
            categories.Add(new SettingCategoryData(AiCategory, CreateSubCategories()));
            return categories.ToArray();
        }

        private static SubCategoryInfo[] CreateSubCategories()
        {
            return new[]
            {
                new SubCategoryInfo(ModelSubCategory, ModelTitle),
                new SubCategoryInfo(ExperienceSubCategory, ExperienceTitle),
                new SubCategoryInfo(VoiceSubCategory, VoiceTitle),
                new SubCategoryInfo(ImageSubCategory, ImageTitle),
                new SubCategoryInfo(ConversationSubCategory, ConversationTitle),
                new SubCategoryInfo(WorldBookSubCategory, WorldBookTitle),
                new SubCategoryInfo(MonthlySubCategory, MonthlyTitle),
                new SubCategoryInfo(AssistantSubCategory, AssistantTitle),
                new SubCategoryInfo(AboutSubCategory, AboutTitle)
            };
        }
    }
}
