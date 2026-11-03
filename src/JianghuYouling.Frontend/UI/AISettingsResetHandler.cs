using System;
using Game.Views.SystemSetting;
using HarmonyLib;
using UnityEngine;

namespace JianghuYouling
{
    internal static class AISettingsResetHandler
    {
        private static bool IsCustom(ESettingSubCategory subCategory)
        {
            int value = (int)subCategory;
            return value >= (int)AISettingsCategorySync.AssistantSubCategory
                && value <= (int)AISettingsCategorySync.AboutSubCategory;
        }

        private static void ResetCustom(ESettingSubCategory subCategory)
        {
            if (subCategory == AISettingsCategorySync.ModelSubCategory) AISettingsModelPane.ResetToDefault();
            else if (subCategory == AISettingsCategorySync.ExperienceSubCategory) AISettingsExperiencePane.ResetToDefault();
            else if (subCategory == AISettingsCategorySync.VoiceSubCategory) AISettingsVoicePane.ResetToDefault();
            else if (subCategory == AISettingsCategorySync.ImageSubCategory) AISettingsImagePane.ResetToDefault();
            else if (subCategory == AISettingsCategorySync.ConversationSubCategory) ResetConversation();
            else if (subCategory == AISettingsCategorySync.WorldBookSubCategory) AISettingsWorldBookEditor.ResetToDefault();
            else if (subCategory == AISettingsCategorySync.MonthlySubCategory) ResetMonthly();
            else if (subCategory == AISettingsCategorySync.AssistantSubCategory) ResetAssistant();
        }

        private static void ResetConversation()
        {
            Check(DifficultyStore.Save("均衡"), "difficulty");
            Check(ReplyLenStore.Save(1), "reply_length");
            Check(UiFontSizeStore.Save(UiFontSizeStore.Default), "font_size");
            Check(TaiwuVoiceStore.Save(""), "taiwu_voice");
            Check(GhostwriteLengthStore.Save(GhostwriteLengthStore.Default), "ghostwrite_length");
            Check(GhostwriteSelfLearningStore.Save(true), "ghostwrite_learning");
            Check(StreamStore.Save(true), "stream");
            Check(ThinkingStore.Save(true), "thinking");
            Check(NpcProactiveChatStore.SaveEnabled(true), "npc_proactive_enabled");
            Check(NpcProactiveChatStore.SaveFrequency(NpcProactiveChatStore.DefaultFrequency),
                "npc_proactive_frequency");
            Check(ThinkingPromptRuleStore.Reset(), "thinking_prompt");
            TalkOrchestrator.Difficulty = DifficultyStore.Load();
            TalkOrchestrator.ReplyLength = ReplyLenStore.Load();
            TalkOrchestrator.TaiwuVoice = TaiwuVoiceStore.Load();
            TalkOrchestrator.StreamingEnabled = StreamStore.Load();
            ChatWindow.ShowThinking = ThinkingStore.Load();
            NpcProactiveChatScheduler.NotifySettingsChanged();
        }

        private static void ResetMonthly()
        {
            Check(AiEventStore.Save(true), "ai_event");
            Check(CompanionMonthlyStore.Save(true), "companion_monthly");
            Check(MonthlyDigestPopupStore.Save(false), "monthly_popup");
            Check(CompanionMonthlyStore.SaveCount(CompanionMonthlyStore.DefaultCount), "monthly_count");
            int taiwuId = -1;
            try { taiwuId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId; } catch { }
            if (taiwuId > 0) Check(CompanionMonthlyCandidateStore.Reset(taiwuId), "monthly_candidates");
            MonthlyDigestPopup.NotifySettingsChanged();
        }

        private static void ResetAssistant()
        {
            Check(AssistantEnabledStore.Save(true), "assistant_enabled");
            Check(AssistantProactiveStore.Save(AssistantProactiveStore.Low), "assistant_proactive");
            Check(AssistantCommissionIntervalStore.Save(AssistantCommissionIntervalStore.Default),
                "assistant_commission");
            Check(AssistantNameStore.Save(AssistantNameStore.Default), "assistant_name");
            Check(AssistantFacePathStore.Clear(), "assistant_face");
            Check(AssistantPersonaStore.SaveDisplay(""), "assistant_persona");
            AssistantWidget.NotifyFacePathChanged();
            AssistantWidget.NotifySettingsChanged();
        }

        private static void Check(bool saved, string field)
        {
            if (!saved) Debug.LogError("[JHYL_AI_SETTINGS] reset field=" + field + " ok=false");
        }

        [HarmonyPatch(typeof(ViewSystemSetting), "CanResetCategory")]
        private static class CanResetCategoryPatch
        {
            private static void Postfix(ESettingSubCategory subCategory, ref bool __result)
            {
                if (IsCustom(subCategory))
                    __result = subCategory != AISettingsCategorySync.AboutSubCategory;
            }
        }

        [HarmonyPatch(typeof(ViewSystemSetting), "ResetCategory")]
        private static class ResetCategoryPatch
        {
            private static bool Prefix(ESettingSubCategory subCategory, bool isShowConfirm, Action onConfirm)
            {
                if (!IsCustom(subCategory) || isShowConfirm) return true;
                ResetCustom(subCategory);
                onConfirm?.Invoke();
                return false;
            }
        }
    }
}
