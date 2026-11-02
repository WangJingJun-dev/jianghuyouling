using System;
using System.Collections.Generic;
using System.IO;
using Game.Views.SystemSetting;
using Newtonsoft.Json.Linq;

namespace JianghuYouling
{
    internal static class AISettingsModelProfileView
    {
        internal const LanguageKey SelectTitle = (LanguageKey)6160;
        internal const LanguageKey NameTitle = (LanguageKey)6161;
        internal const LanguageKey SaveTitle = (LanguageKey)6162;
        internal const LanguageKey ActivateTitle = (LanguageKey)6163;
        internal const LanguageKey DeleteTitle = (LanguageKey)6164;
        internal const LanguageKey StatusTitle = (LanguageKey)6165;
        internal const LanguageKey SaveActionText = (LanguageKey)6166;
        internal const LanguageKey ActivateActionText = (LanguageKey)6167;
        internal const LanguageKey DeleteActionText = (LanguageKey)6168;
        internal const LanguageKey EmptyText = (LanguageKey)6169;
        private static readonly List<string> Names = new List<string>();
        private static readonly Dictionary<ushort, string> DynamicText = new Dictionary<ushort, string>();
        private static int _selectedIndex;
        private static string _profileName = string.Empty;
        private static string _pendingDelete;
        internal static string Status { get; private set; } = "模型配置档案使用 DPAPI 独立加密保存";

        internal static List<ISettingItemInfo> CreateInfos()
        {
            RefreshNames();
            ESettingSubCategory sub = AISettingsCategorySync.ModelSubCategory;
            var infos = new List<ISettingItemInfo>
            {
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 1, SelectTitle)
                    { Options = BuildOptions() }, () => (sbyte)_selectedIndex, Select),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 2, NameTitle)
                    { CharacterLimit = LlmProfileStore.MaxNameChars }, () => _profileName,
                    value => _profileName = (value ?? "").Trim()),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 3, SaveTitle)
                    { ActionText = SaveActionText }, () => 0, _ => SaveProfile()),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 4, ActivateTitle)
                    { ActionText = ActivateActionText }, () => 0, _ => ActivateProfile()),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 5, DeleteTitle)
                    { ActionText = DeleteActionText }, () => 0, _ => DeleteProfile()),
                new SettingItemInfo<string>(new NativeReadonlyTextSettingAttribute(sub, 6, StatusTitle)
                    { Height = 64f }, () => Status, _ => { })
            };
            return infos;
        }

        private static void RefreshNames()
        {
            Names.Clear();
            DynamicText.Clear();
            IReadOnlyList<string> names = LlmProfileStore.List(out string selected, out string error);
            if (error == null && names != null)
                foreach (string name in names) Names.Add(name);
            if (Names.Count == 0) Names.Add(string.Empty);
            _selectedIndex = 0;
            for (int i = 0; i < Names.Count; i++)
            {
                if (string.Equals(Names[i], selected, StringComparison.OrdinalIgnoreCase)) _selectedIndex = i;
                DynamicText[(ushort)(6200 + i)] = Names[i];
            }
            if (Names.Count == 1 && Names[0].Length == 0) _selectedIndex = 0;
        }

        private static LanguageKey[] BuildOptions()
        {
            var options = new LanguageKey[Names.Count];
            for (int i = 0; i < options.Length; i++)
                options[i] = string.IsNullOrEmpty(Names[i]) ? EmptyText : (LanguageKey)(6200 + i);
            return options;
        }

        internal static string Localize(ushort id)
        {
            return DynamicText.TryGetValue(id, out string value) && !string.IsNullOrEmpty(value) ? value : null;
        }

        private static void Select(sbyte index)
        {
            if (index >= 0 && index < Names.Count) _selectedIndex = index;
            _pendingDelete = null;
        }

        private static string SelectedName()
            => _selectedIndex >= 0 && _selectedIndex < Names.Count ? Names[_selectedIndex] : string.Empty;

        private static void SaveProfile()
        {
            string name = (_profileName ?? "").Trim();
            JObject config = AISettingsModelPane.BuildCurrentConfig(out string apiKey, out string error);
            if (config == null)
            {
                Status = "档案未保存：当前模型配置无效";
                AISettingsHost.QueueRefresh();
                return;
            }
            if (!LlmProfileStore.Save(name, config, apiKey, out error))
            {
                Status = "档案未保存：" + (error ?? "磁盘写入失败");
                AISettingsHost.QueueRefresh();
                return;
            }
            _profileName = string.Empty;
            Status = "已加密保存模型配置档案「" + name + "」";
            AISettingsHost.QueueRefresh();
        }

        private static void ActivateProfile()
        {
            string name = SelectedName();
            string error = null;
            if (string.IsNullOrEmpty(name) || !LlmProfileStore.TryLoad(name, out JObject config,
                    out string apiKey, out error))
            {
                Status = "切换失败：" + (error ?? "没有可切换的档案");
                AISettingsHost.QueueRefresh();
                return;
            }
            if (!AISettingsModelPane.ValidateConfig(config, apiKey, out error))
            {
                Status = "切换失败：档案内容无效";
                AISettingsHost.QueueRefresh();
                return;
            }
            SettingsBatchTransaction transaction = null;
            try
            {
                transaction = SettingsBatchTransaction.Capture(new[]
                {
                    Path.Combine(JianghuYoulingPaths.Settings, "llm.json"),
                    LlmProfileStore.IndexPath
                });
                if (!LlmService.SaveRaw(config, apiKey, out error) || !LlmProfileStore.Select(name, out error))
                {
                    bool rollbackOk = transaction.Rollback();
                    LlmService.Reload();
                    Status = rollbackOk ? "切换失败：已恢复原配置" : "切换失败：回滚未完整确认，请检查实际配置";
                    AISettingsHost.QueueRefresh();
                    return;
                }
                transaction.Commit();
                Status = "已切换到模型配置档案「" + name + "」";
            }
            catch (Exception)
            {
                bool rollbackOk = transaction != null && transaction.Rollback();
                LlmService.Reload();
                Status = rollbackOk ? "切换失败：已恢复原配置" : "切换失败：回滚未完整确认，请检查实际配置";
            }
            finally { transaction?.Dispose(); }
            AISettingsHost.QueueRefresh();
        }

        private static void DeleteProfile()
        {
            string name = SelectedName();
            if (string.IsNullOrEmpty(name))
            {
                Status = "没有可删除的档案";
                AISettingsHost.QueueRefresh();
                return;
            }
            if (!string.Equals(_pendingDelete, name, StringComparison.Ordinal))
            {
                _pendingDelete = name;
                Status = "再次点击「删除档案」即可删除「" + name + "」";
                AISettingsHost.QueueRefresh();
                return;
            }
            _pendingDelete = null;
            Status = LlmProfileStore.Delete(name, out string next, out string error)
                ? "已删除模型配置档案「" + name + "」"
                : "删除失败：" + (error ?? "磁盘写入失败");
            AISettingsHost.QueueRefresh();
        }
    }
}
