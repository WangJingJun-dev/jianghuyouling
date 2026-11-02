using System.Collections.Generic;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Behavior;

namespace JianghuYouling
{
    internal static class AISettingsExperiencePane
    {
        internal const LanguageKey PresetTitle = (LanguageKey)6186;
        internal const LanguageKey ApplyTitle = (LanguageKey)6187;
        internal const LanguageKey ApplyActionText = (LanguageKey)6188;
        internal const LanguageKey SummaryTitle = (LanguageKey)6189;
        internal const LanguageKey EconomyText = (LanguageKey)6300;
        internal const LanguageKey BalancedText = (LanguageKey)6301;
        internal const LanguageKey BestText = (LanguageKey)6302;
        private static int _selectedIndex = 1;
        private static string _status;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            ESettingSubCategory sub = AISettingsCategorySync.ExperienceSubCategory;
            return new List<ISettingItemInfo>
            {
                new SettingItemInfo<sbyte>(new NativeExperiencePresetSettingAttribute(sub, 1, PresetTitle),
                    () => (sbyte)_selectedIndex, Select),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 2, ApplyTitle)
                    { ActionText = ApplyActionText }, () => 0, _ => Apply()),
                new SettingItemInfo<string>(new NativeReadonlyTextSettingAttribute(sub, 3, SummaryTitle)
                    { Height = 64f }, Summary, _ => { })
            };
        }

        private static void Select(sbyte value)
        {
            if (value < 0 || value > 2) return;
            _selectedIndex = value;
        }

        private static string Summary()
        {
            if (_status != null) return _status;
            ExperiencePresetDefinition matched = ExperiencePresetPolicy.Match(
                AiEventStore.Load(), CompanionMonthlyStore.Load(), CompanionMonthlyStore.LoadCount(),
                GroupRoundsStore.Load(), AssistantProactiveStore.Load());
            return matched == null ? "当前为自定义组合；应用档位只会修改五项体验设置。"
                : "当前档位：" + matched.DisplayName + "；模型、密钥、长文本与世界书不受影响。";
        }

        internal static void ResetToDefault()
        {
            _selectedIndex = 1;
            _status = null;
            Apply();
        }

        private static void Apply()
        {
            string id = _selectedIndex == 0 ? ExperiencePresetPolicy.EconomyId
                : _selectedIndex == 2 ? ExperiencePresetPolicy.BestId : ExperiencePresetPolicy.BalancedId;
            if (!ExperiencePresetPolicy.TryGet(id, out ExperiencePresetDefinition definition)) return;
            SettingsBatchTransaction transaction;
            try
            {
                transaction = SettingsBatchTransaction.Capture(new[]
                {
                    AiEventStore.SettingsPath,
                    CompanionMonthlyStore.SettingsPath,
                    CompanionMonthlyStore.CountPath,
                    GroupRoundsStore.SettingsPath,
                    AssistantProactiveStore.SettingsPath
                });
            }
            catch
            {
                _status = "无法建立档位保存事务，未修改任何设置";
                AISettingsHost.QueueRefresh();
                return;
            }
            bool saved;
            bool rollbackOk = true;
            using (transaction)
            {
                saved = AiEventStore.Save(definition.MonthlyEvent);
                if (saved) saved = CompanionMonthlyStore.Save(definition.CompanionMonthly);
                if (saved) saved = CompanionMonthlyStore.SaveCount(definition.CompanionCount);
                if (saved) saved = GroupRoundsStore.Save(definition.GroupRounds);
                if (saved) saved = AssistantProactiveStore.Save(definition.AssistantFrequency);
                if (saved) transaction.Commit();
                else rollbackOk = transaction.Rollback();
            }
            _status = saved ? "已应用「" + definition.DisplayName + "」，五项设置已同步"
                : rollbackOk ? "档位未能完整保存，已回滚并恢复实际设置"
                    : "档位保存失败，且回滚未完整确认，请检查磁盘实际设置";
            if (saved) AssistantWidget.NotifySettingsChanged();
            AISettingsHost.QueueRefresh();
        }
    }
}
