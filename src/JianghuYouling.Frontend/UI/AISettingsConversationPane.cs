using System;
using System.Collections.Generic;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Prompt;

namespace JianghuYouling
{
    internal static class AISettingsConversationPane
    {
        internal const LanguageKey DifficultyTitle = (LanguageKey)6120;
        internal const LanguageKey ReplyLengthTitle = (LanguageKey)6121;
        internal const LanguageKey FontSizeTitle = (LanguageKey)6122;
        internal const LanguageKey TaiwuVoiceTitle = (LanguageKey)6123;
        internal const LanguageKey GhostwriteLengthTitle = (LanguageKey)6124;
        internal const LanguageKey GhostwriteLearningTitle = (LanguageKey)6125;
        internal const LanguageKey StreamTitle = (LanguageKey)6126;
        internal const LanguageKey ThinkingTitle = (LanguageKey)6127;
        internal const LanguageKey ProactiveEnabledTitle = (LanguageKey)6129;
        internal const LanguageKey ProactiveFrequencyTitle = (LanguageKey)6130;
        internal const LanguageKey ThinkingPromptTitle = (LanguageKey)6131;
        internal const LanguageKey EasyText = (LanguageKey)6132;
        internal const LanguageKey BalancedText = (LanguageKey)6133;
        internal const LanguageKey HardText = (LanguageKey)6134;
        internal const LanguageKey ShortText = (LanguageKey)6135;
        internal const LanguageKey MediumText = (LanguageKey)6136;
        internal const LanguageKey DetailedText = (LanguageKey)6137;
        internal const LanguageKey UnlimitedText = (LanguageKey)6138;
        internal const LanguageKey SmallText = (LanguageKey)6139;
        internal const LanguageKey FontMediumText = (LanguageKey)6140;
        internal const LanguageKey LargeText = (LanguageKey)6141;
        internal const LanguageKey VeryLowText = (LanguageKey)6145;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            ESettingSubCategory sub = AISettingsCategorySync.ConversationSubCategory;
            return new List<ISettingItemInfo>
            {
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 1, DifficultyTitle)
                    { Options = new[] { EasyText, BalancedText, HardText } },
                    () => (sbyte)Math.Max(0, Array.IndexOf(DifficultyStore.Options, DifficultyStore.Load())), value =>
                    {
                        string difficulty = value == 0 ? "简单" : value == 2 ? "困难" : "均衡";
                        if (AISettingsSave.Checked(DifficultyStore.Save(difficulty), "difficulty"))
                            TalkOrchestrator.Difficulty = DifficultyStore.Load();
                    }),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 2, ReplyLengthTitle)
                    { Options = new[] { ShortText, MediumText, DetailedText, UnlimitedText } },
                    () => (sbyte)ReplyLenStore.Load(), value =>
                    {
                        if (AISettingsSave.Checked(ReplyLenStore.Save(value), "reply_length"))
                            TalkOrchestrator.ReplyLength = ReplyLenStore.Load();
                    }),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 3, FontSizeTitle)
                    { Options = new[] { SmallText, FontMediumText, LargeText } },
                    () => (sbyte)UiFontSizeStore.Current,
                    value => AISettingsSave.Checked(UiFontSizeStore.Save(value), "font_size")),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 4, TaiwuVoiceTitle)
                    { CharacterLimit = 32768 }, TaiwuVoiceStore.Load, value =>
                    {
                        Require(TaiwuVoiceStore.Save(value), "taiwu_voice_save_failed");
                        TalkOrchestrator.TaiwuVoice = TaiwuVoiceStore.Load();
                    }),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 5, GhostwriteLengthTitle)
                    { Options = new[] { ShortText, MediumText, DetailedText } },
                    () => (sbyte)GhostwriteLengthStore.Load(),
                    value => AISettingsSave.Checked(GhostwriteLengthStore.Save(value), "ghostwrite_length")),
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 6, GhostwriteLearningTitle),
                    GhostwriteSelfLearningStore.Load,
                    value => AISettingsSave.Checked(GhostwriteSelfLearningStore.Save(value), "ghostwrite_learning")),
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 7, StreamTitle), StreamStore.Load, value =>
                {
                    if (AISettingsSave.Checked(StreamStore.Save(value), "stream"))
                        TalkOrchestrator.StreamingEnabled = StreamStore.Load();
                }),
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 8, ThinkingTitle), ThinkingStore.Load, value =>
                {
                    if (AISettingsSave.Checked(ThinkingStore.Save(value), "thinking"))
                        ChatWindow.ShowThinking = ThinkingStore.Load();
                }),
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 9, ProactiveEnabledTitle),
                    NpcProactiveChatStore.LoadEnabled, value =>
                    {
                        if (AISettingsSave.Checked(NpcProactiveChatStore.SaveEnabled(value), "npc_proactive_enabled"))
                            NpcProactiveChatScheduler.NotifySettingsChanged();
                    }),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 10, ProactiveFrequencyTitle)
                    { Options = new[] { VeryLowText, AISettingsAssistantPane.LowText,
                        AISettingsAssistantPane.MidText, AISettingsAssistantPane.HighText } },
                    () => (sbyte)NpcProactiveChatStore.LoadFrequency(), value =>
                    {
                        if (AISettingsSave.Checked(NpcProactiveChatStore.SaveFrequency(value), "npc_proactive_frequency"))
                            NpcProactiveChatScheduler.NotifySettingsChanged();
                    }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 11, ThinkingPromptTitle)
                    { Multiline = true, CharacterLimit = ThinkingPromptRule.MaxPromptChars },
                    ThinkingPromptRuleStore.Load, value =>
                        Require(ThinkingPromptRuleStore.Save(value), "thinking_prompt_save_failed"))
            };
        }

        private static void Require(bool saved, string error)
        {
            if (!saved) throw new InvalidOperationException(error);
        }
    }
}
