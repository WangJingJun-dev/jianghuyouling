using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Views.SystemSetting;
using HarmonyLib;
using JianghuYouling.Effects;
using UnityEngine;

namespace JianghuYouling
{
    internal static class AISettingsMonthlyCandidateView
    {
        internal const LanguageKey SelectedTitle = (LanguageKey)6340;
        internal const LanguageKey ToggleTitle = (LanguageKey)6341;
        internal const LanguageKey StatusTitle = (LanguageKey)6342;
        internal const LanguageKey ToggleActionText = (LanguageKey)6343;
        internal const LanguageKey EmptyText = (LanguageKey)6344;
        private static readonly List<Candidate> Candidates = new List<Candidate>();
        private static readonly Dictionary<ushort, string> DynamicText = new Dictionary<ushort, string>();
        private static int _selectedIndex;
        private static int _taiwuId;
        private static uint _worldId;
        private static int _generation = -1;
        private static long _requestEpoch;
        private static bool _loading;
        private static bool _loaded;
        private static string _status = "请先进入存档，再管理主动人物名单";

        internal static bool IsAvailable
            => !_loading && WorldLifecycle.HasWorldIdentity && ResolveTaiwuId() > 0;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            EnsureContext();
            ESettingSubCategory sub = AISettingsCategorySync.MonthlySubCategory;
            return new List<ISettingItemInfo>
            {
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 5, SelectedTitle)
                    { Options = CandidateOptions() }, () => (sbyte)_selectedIndex,
                    value => { if (value >= 0 && value < Candidates.Count) _selectedIndex = value; }),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 6, ToggleTitle)
                    { ActionText = ToggleActionText }, () => 0, _ => ToggleSelected())
            };
        }

        internal static string Localize(ushort id)
            => DynamicText.TryGetValue(id, out string value) ? value : null;

        private static void EnsureContext()
        {
            DynamicText.Clear();
            int taiwuId = ResolveTaiwuId();
            if (!WorldLifecycle.HasWorldIdentity || taiwuId <= 0)
            {
                _taiwuId = 0;
                Candidates.Clear();
                _status = "请先进入存档，再管理主动人物名单";
                return;
            }
            if (taiwuId != _taiwuId || WorldLifecycle.WorldId != _worldId
                || WorldLifecycle.Generation != _generation || (!_loading && !_loaded))
                BeginLoad(taiwuId);
        }

        private static int ResolveTaiwuId()
        {
            try { return SingletonObject.getInstance<BasicGameData>().TaiwuCharId; } catch { return -1; }
        }

        private static LanguageKey[] CandidateOptions()
        {
            if (Candidates.Count == 0)
            {
                DynamicText[(ushort)EmptyText] = "暂无可显示人物";
                return new[] { EmptyText };
            }
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Candidates.Count - 1);
            var options = new LanguageKey[Candidates.Count];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = (LanguageKey)(7500 + i);
                Candidate candidate = Candidates[i];
                DynamicText[(ushort)options[i]] = candidate.Name
                    + (candidate.Companion ? "（同道）" : "（手动）")
                    + (candidate.Active ? "" : "（已移除）");
            }
            return options;
        }

        private static void BeginLoad(int taiwuId)
        {
            if (_loading || ConfigHost.Instance == null) return;
            _loading = true;
            _loaded = false;
            _taiwuId = taiwuId;
            _worldId = WorldLifecycle.WorldId;
            _generation = WorldLifecycle.Generation;
            long epoch = unchecked(++_requestEpoch);
            _status = "正在读取主动人物名单……";
            ConfigHost.Instance.StartCoroutine(LoadRows(epoch));
        }

        private static bool IsCurrent(long epoch)
            => epoch == _requestEpoch && _taiwuId > 0 && _worldId > 0
                && WorldLifecycle.IsSameWorld(_generation) && WorldLifecycle.WorldId == _worldId;

        private static IEnumerator LoadRows(long epoch)
        {
            List<int> team = null;
            bool teamDone = false, teamOk = false;
            EffectHandler.QueryNonBabyCompanionGroup(_taiwuId, (ok, ids) =>
            {
                if (!IsCurrent(epoch)) return;
                teamOk = ok;
                team = ids;
                teamDone = true;
            });
            float deadline = Time.unscaledTime + EffectHandler.ReadOnlyQueryWaitSeconds;
            while (!teamDone && Time.unscaledTime < deadline && IsCurrent(epoch)) yield return null;
            if (!IsCurrent(epoch)) yield break;
            if (!teamDone || !teamOk)
            {
                FinishLoad(epoch, "同道名单读取失败，请稍后重试");
                yield break;
            }

            var stored = CompanionMonthlyCandidateStore.Load(_taiwuId);
            List<int> manual = null;
            bool manualDone = false, manualOk = false;
            EffectHandler.QueryMonthlyAgentEligibility(stored.Included, (ok, ids, _) =>
            {
                if (!IsCurrent(epoch)) return;
                manualOk = ok;
                manual = ids;
                manualDone = true;
            });
            deadline = Time.unscaledTime + EffectHandler.ReadOnlyQueryWaitSeconds;
            while (!manualDone && Time.unscaledTime < deadline && IsCurrent(epoch)) yield return null;
            if (!IsCurrent(epoch)) yield break;
            if (!manualDone || !manualOk)
            {
                FinishLoad(epoch, "手动加入名单读取失败，请稍后重试");
                yield break;
            }

            team = team ?? new List<int>();
            manual = manual ?? new List<int>();
            List<int> effective = CompanionMonthlyCandidateStore.BuildEffective(_taiwuId, team, manual);
            var excluded = new HashSet<int>(stored.Excluded);
            var ids = new SortedSet<int>(effective);
            foreach (int id in team) if (excluded.Contains(id)) ids.Add(id);
            List<int> ordered = ids.ToList();
            List<string> names = null;
            bool namesDone = ordered.Count == 0;
            if (ordered.Count > 0)
                EffectHandler.QueryCharNames(ordered, value =>
                {
                    if (!IsCurrent(epoch)) return;
                    names = value;
                    namesDone = true;
                });
            deadline = Time.unscaledTime + EffectHandler.ReadOnlyQueryWaitSeconds;
            while (!namesDone && Time.unscaledTime < deadline && IsCurrent(epoch)) yield return null;
            if (!IsCurrent(epoch)) yield break;
            if (!namesDone)
            {
                FinishLoad(epoch, "人物姓名读取失败，请稍后重试");
                yield break;
            }

            var teamSet = new HashSet<int>(team);
            Candidates.Clear();
            for (int i = 0; i < ordered.Count; i++)
            {
                int id = ordered[i];
                string name = names != null && i < names.Count && !string.IsNullOrWhiteSpace(names[i])
                    ? names[i] : "人物#" + id;
                Candidates.Add(new Candidate
                {
                    Id = id,
                    Name = name,
                    Companion = teamSet.Contains(id),
                    Active = !excluded.Contains(id)
                });
            }
            FinishLoad(epoch, Candidates.Count == 0
                ? "当前名单为空，可在人物杂务中加入主动人物"
                : "共 " + Candidates.Count + " 人；可移除或重新加入同道");
        }

        private static void FinishLoad(long epoch, string status)
        {
            if (!IsCurrent(epoch)) return;
            _loading = false;
            _loaded = true;
            _status = status;
            AISettingsHost.QueueRefresh();
        }

        private static void ToggleSelected()
        {
            if (_selectedIndex < 0 || _selectedIndex >= Candidates.Count || !IsAvailable) return;
            Candidate candidate = Candidates[_selectedIndex];
            bool saved = candidate.Active
                ? CompanionMonthlyCandidateStore.Remove(_taiwuId, candidate.Id, candidate.Companion)
                : CompanionMonthlyCandidateStore.Add(_taiwuId, candidate.Id);
            if (!saved)
            {
                _status = "名单写入失败，磁盘状态未确认";
                AISettingsHost.QueueRefresh();
                return;
            }
            ChatWindow.NotifyMonthlyCandidateChanged(_taiwuId, candidate.Id);
            _status = candidate.Active ? "已移除「" + candidate.Name + "」" : "已重新加入「" + candidate.Name + "」";
            BeginLoad(_taiwuId);
            AISettingsHost.QueueRefresh();
        }

        internal static void ResetForWorldExit()
        {
            unchecked { _requestEpoch++; }
            _loading = false;
            _loaded = false;
            _taiwuId = 0;
            _worldId = 0;
            _generation = -1;
            Candidates.Clear();
            _status = "请先进入存档，再管理主动人物名单";
        }

        private sealed class Candidate
        {
            public int Id;
            public string Name;
            public bool Companion;
            public bool Active;
        }
    }

    [HarmonyPatch(typeof(SettingGroupItem), "UpdateItemInteractableState")]
    internal static class AISettingsMonthlyCandidateInteractablePatch
    {
        private static void Postfix(SettingItemBase item, ISettingItemInfo info)
        {
            if (info.Attribute.SubCategory == AISettingsCategorySync.MonthlySubCategory
                && info.Attribute.Order >= 5)
                item.SetInteractable(AISettingsMonthlyCandidateView.IsAvailable);
        }
    }
}
