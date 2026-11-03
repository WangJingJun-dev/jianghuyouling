using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Prompt;
using UnityEngine;

namespace JianghuYouling
{
    internal static class AISettingsWorldBookEditor
    {
        internal const LanguageKey CategoryFilterTitle = (LanguageKey)6310;
        internal const LanguageKey EntryTitle = (LanguageKey)6311;
        internal const LanguageKey EnabledTitle = (LanguageKey)6312;
        internal const LanguageKey ModeTitle = (LanguageKey)6313;
        internal const LanguageKey PriorityTitle = (LanguageKey)6314;
        internal const LanguageKey NameTitle = (LanguageKey)6315;
        internal const LanguageKey CategoryTitle = (LanguageKey)6316;
        internal const LanguageKey KeywordsTitle = (LanguageKey)6317;
        internal const LanguageKey ContentTitle = (LanguageKey)6318;
        internal const LanguageKey SaveTitle = (LanguageKey)6319;
        internal const LanguageKey AddTitle = (LanguageKey)6320;
        internal const LanguageKey DeleteTitle = (LanguageKey)6321;
        internal const LanguageKey ImportPathTitle = (LanguageKey)6323;
        internal const LanguageKey ImportTitle = (LanguageKey)6324;
        internal const LanguageKey ExportTitle = (LanguageKey)6325;
        internal const LanguageKey OpenExchangeTitle = (LanguageKey)6326;
        internal const LanguageKey StatusTitle = (LanguageKey)6327;
        internal const LanguageKey SaveActionText = (LanguageKey)6328;
        internal const LanguageKey AddActionText = (LanguageKey)6329;
        internal const LanguageKey DeleteActionText = (LanguageKey)6330;
        internal const LanguageKey ImportActionText = (LanguageKey)6332;
        internal const LanguageKey ExportActionText = (LanguageKey)6333;
        internal const LanguageKey OpenExchangeActionText = (LanguageKey)6334;
        internal const LanguageKey AlwaysText = (LanguageKey)6335;
        internal const LanguageKey KeywordText = (LanguageKey)6336;
        internal const LanguageKey EmptyText = (LanguageKey)6337;
        internal const LanguageKey AllCategoriesText = (LanguageKey)6338;
        private const string OperationOwner = "ai-settings-worldbook";
        private static readonly Dictionary<ushort, string> DynamicText = new Dictionary<ushort, string>();
        private static readonly List<StructuredWorldBook.Entry> VisibleEntries = new List<StructuredWorldBook.Entry>();
        private static StructuredWorldBook.Document _document;
        private static StructuredWorldBook.Entry _selected;
        private static string _categoryFilter = StructuredWorldBook.ReservedAllCategory;
        private static string _status = "请先进入存档，再编辑世界书";
        private static string _importPath = string.Empty;
        private static int _taiwuId;
        private static int _contextVersion;
        private static int _categoryIndex;
        private static int _entryIndex;
        private static long _operationToken;
        private static long _draftVersion;
        private static bool _busy;

        internal static bool IsAvailable
            => !_busy && WorldLifecycle.HasWorldIdentity && ResolveTaiwuId() > 0;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            EnsureContext();
            ESettingSubCategory sub = AISettingsCategorySync.WorldBookSubCategory;
            return new List<ISettingItemInfo>
            {
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 1, CategoryFilterTitle)
                    { Options = CategoryOptions() }, () => (sbyte)_categoryIndex, SelectCategory),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 2, EntryTitle)
                    { Options = EntryOptions() }, () => (sbyte)_entryIndex, SelectEntry),
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 3, EnabledTitle),
                    () => _selected != null && _selected.Enabled, value =>
                    {
                        if (_selected != null) { _selected.Enabled = value; MarkDraft(); }
                    }),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 4, ModeTitle)
                    { Options = new[] { AlwaysText, KeywordText } },
                    () => (sbyte)(_selected != null && _selected.Mode == StructuredWorldBook.ModeKeyword ? 1 : 0),
                    value =>
                    {
                        if (_selected != null)
                        {
                            _selected.Mode = value == 1 ? StructuredWorldBook.ModeKeyword : StructuredWorldBook.ModeAlways;
                            MarkDraft();
                        }
                    }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 5, PriorityTitle)
                    { CharacterLimit = 6 }, () => _selected?.Priority.ToString() ?? "100", SetPriority),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 6, NameTitle)
                    { CharacterLimit = StructuredWorldBook.MaxNameChars }, () => _selected?.Name ?? "",
                    value => { if (_selected != null) { _selected.Name = value; MarkDraft(); } }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 7, CategoryTitle)
                    { CharacterLimit = StructuredWorldBook.MaxCategoryChars }, () => _selected?.Category ?? "",
                    value => { if (_selected != null) { _selected.Category = value; MarkDraft(); } }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 8, KeywordsTitle)
                    { CharacterLimit = StructuredWorldBook.MaxKeywordsChars }, () => _selected?.Keywords ?? "",
                    value => { if (_selected != null) { _selected.Keywords = value; MarkDraft(); } }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 9, ContentTitle)
                    { Multiline = true, CharacterLimit = WorldBookStore.MaxCustomWorldBookChars },
                    () => _selected?.Content ?? "", value => { if (_selected != null) { _selected.Content = value; MarkDraft(); } }),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 10, SaveTitle)
                    { ActionText = SaveActionText }, () => 0, _ => Save()),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 11, AddTitle)
                    { ActionText = AddActionText }, () => 0, _ => AddEntry()),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 12, DeleteTitle)
                    { ActionText = DeleteActionText }, () => 0, _ => DeleteEntry()),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 13, ImportPathTitle)
                    { CharacterLimit = 4096 }, () => _importPath, value => _importPath = (value ?? "").Trim()),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 14, ImportTitle)
                    { ActionText = ImportActionText }, () => 0, _ => Import()),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 15, ExportTitle)
                    { ActionText = ExportActionText }, () => 0, _ => Export()),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 16, OpenExchangeTitle)
                    { ActionText = OpenExchangeActionText }, () => 0, _ => OpenExchangeDirectory())
            };
        }

        internal static string Localize(ushort id)
            => DynamicText.TryGetValue(id, out string value) ? value : null;

        internal static string CurrentStatus => _status;

        private static void EnsureContext()
        {
            DynamicText.Clear();
            if (!WorldLifecycle.HasWorldIdentity)
            {
                _taiwuId = 0;
                _document = null;
                _selected = null;
                _status = "请先进入存档，再编辑世界书";
                return;
            }
            int taiwuId = ResolveTaiwuId();
            if (taiwuId <= 0)
            {
                _taiwuId = 0;
                _document = null;
                _selected = null;
                _status = "请先进入存档，再编辑世界书";
                return;
            }
            if (taiwuId != _taiwuId || (_document == null && !_busy)) BeginLoad(taiwuId);
        }

        private static int ResolveTaiwuId()
        {
            try { return SingletonObject.getInstance<BasicGameData>().TaiwuCharId; } catch { return -1; }
        }

        private static LanguageKey[] CategoryOptions()
        {
            var categories = new List<string> { StructuredWorldBook.ReservedAllCategory };
            if (_document != null)
            {
                categories.AddRange(_document.Categories ?? new List<string>());
                categories.AddRange(_document.Entries.Select(entry => entry.Category));
            }
            categories = categories.Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct().ToList();
            if (categories.Count == 0) categories.Add(StructuredWorldBook.ReservedAllCategory);
            _categoryIndex = Math.Max(0, categories.IndexOf(_categoryFilter));
            var options = new LanguageKey[categories.Count];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = categories[i] == StructuredWorldBook.ReservedAllCategory
                    ? AllCategoriesText : (LanguageKey)(6400 + i);
                DynamicText[(ushort)options[i]] = categories[i];
            }
            return options;
        }

        private static LanguageKey[] EntryOptions()
        {
            VisibleEntries.Clear();
            if (_document != null)
                VisibleEntries.AddRange(_document.Entries.Where(entry =>
                    _categoryFilter == StructuredWorldBook.ReservedAllCategory
                    || string.Equals(entry.Category, _categoryFilter, StringComparison.Ordinal)));
            if (_selected == null && VisibleEntries.Count > 0) _selected = VisibleEntries[0];
            _entryIndex = Math.Max(0, VisibleEntries.IndexOf(_selected));
            if (VisibleEntries.Count == 0)
            {
                DynamicText[(ushort)EmptyText] = "暂无条目";
                return new[] { EmptyText };
            }
            var options = new LanguageKey[VisibleEntries.Count];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = (LanguageKey)(6500 + i);
                DynamicText[(ushort)options[i]] = VisibleEntries[i].Name;
            }
            return options;
        }

        private static void SelectCategory(sbyte index)
        {
            var options = CategoryOptions();
            if (index < 0 || index >= options.Length) return;
            _categoryFilter = DynamicText[(ushort)options[index]];
            _selected = null;
            AISettingsHost.QueueRefresh();
        }

        private static void SelectEntry(sbyte index)
        {
            if (index >= 0 && index < VisibleEntries.Count) _selected = VisibleEntries[index];
            AISettingsHost.QueueRefresh();
        }

        private static void SetPriority(string value)
        {
            if (_selected == null) return;
            if (!int.TryParse(value, out int priority) || priority < StructuredWorldBook.MinPriority
                || priority > StructuredWorldBook.MaxPriority)
                throw new InvalidOperationException("worldbook_priority_invalid");
            _selected.Priority = priority;
            MarkDraft();
        }

        private static void MarkDraft()
        {
            unchecked { _draftVersion++; }
        }

        private static void BeginLoad(int taiwuId)
        {
            if (_busy || ConfigHost.Instance == null) return;
            if (!LongTextEditorOperationGate.TryAcquire(OperationOwner, out long token))
            {
                _status = "另一个世界书或人设操作仍在进行，请稍候";
                return;
            }
            _busy = true;
            _operationToken = token;
            AISettingsHost.QueueRefresh();
            _taiwuId = taiwuId;
            int version = unchecked(++_contextVersion);
            int generation = WorldLifecycle.Generation;
            uint worldId = WorldLifecycle.WorldId;
            string directory = JianghuYoulingPaths.Personas;
            _status = "正在读取世界书……";
            Task<StructuredWorldBookStore.Snapshot> task = Task.Run(
                () => StructuredWorldBookStore.LoadFromDirectory(directory, taiwuId));
            ConfigHost.Instance.StartCoroutine(WaitLoad(task, token, version, generation, worldId));
        }

        private static IEnumerator WaitLoad(Task<StructuredWorldBookStore.Snapshot> task,
            long token, int version, int generation, uint worldId)
        {
            while (task != null && !task.IsCompleted) yield return null;
            bool valid = _operationToken == token && version == _contextVersion
                && WorldLifecycle.IsSameWorld(generation) && WorldLifecycle.WorldId == worldId;
            StructuredWorldBookStore.Snapshot result = task != null && !task.IsFaulted ? task.Result : null;
            FinishOperation(token);
            if (!valid) yield break;
            _document = result?.Document;
            _selected = _document?.Entries.FirstOrDefault();
            _status = _document == null ? "世界书读取失败" : "世界书已读取；修改后请点击保存";
            AISettingsHost.QueueRefresh();
        }

        private static void Save()
        {
            if (_busy || _document == null || !IsAvailable) return;
            StructuredWorldBook.Normalize(_document);
            if (!StructuredWorldBook.Validate(_document, out string error)
                || StructuredWorldBook.Compile(_document).Length > WorldBookStore.MaxCustomWorldBookChars)
            {
                _status = "世界书校验失败：" + error;
                AISettingsHost.QueueRefresh();
                return;
            }
            if (!LongTextEditorOperationGate.TryAcquire(OperationOwner, out long token)) return;
            _busy = true;
            _operationToken = token;
            AISettingsHost.QueueRefresh();
            long draft = _draftVersion;
            int generation = WorldLifecycle.Generation;
            uint worldId = WorldLifecycle.WorldId;
            int taiwuId = _taiwuId;
            string directory = JianghuYoulingPaths.Personas;
            StructuredWorldBook.Document captured = CloneDocument(_document);
            _status = "正在保存世界书……";
            Task<OperationResult> task = Task.Run(() => SaveDocument(directory, taiwuId, captured,
                generation, worldId));
            ConfigHost.Instance.StartCoroutine(WaitSave(task, token, draft, generation, worldId));
        }

        private static OperationResult SaveDocument(string directory, int taiwuId,
            StructuredWorldBook.Document document, int generation, uint worldId)
        {
            return WorldBookIoCoordinator.Run(worldId, generation, () =>
            {
                SettingsBatchTransaction transaction = null;
                try
                {
                    transaction = SettingsBatchTransaction.Capture(new[]
                    {
                        Path.Combine(directory, "Worldbook_" + taiwuId + ".txt"),
                        StructuredWorldBookStore.LayoutPath(directory, taiwuId)
                    });
                    if (!StructuredWorldBookStore.SaveFromDirectory(directory, taiwuId, document, out string error))
                    {
                        transaction.Rollback();
                        transaction.Dispose();
                        return new OperationResult { Error = error };
                    }
                    transaction.Commit();
                    transaction.Dispose();
                    return new OperationResult { Document = document };
                }
                catch (Exception ex)
                {
                    transaction?.Rollback();
                    transaction?.Dispose();
                    return new OperationResult { Error = ex.GetType().Name };
                }
            });
        }

        private static IEnumerator WaitSave(Task<OperationResult> task, long token, long draft,
            int generation, uint worldId)
        {
            while (task != null && !task.IsCompleted) yield return null;
            OperationResult result = task != null && !task.IsFaulted ? task.Result : null;
            bool valid = _operationToken == token && WorldLifecycle.IsSameWorld(generation)
                && WorldLifecycle.WorldId == worldId;
            if (!valid)
            {
                FinishOperation(token);
                yield break;
            }
            FinishOperation(token);
            _status = result?.Document != null && result.Error == null
                ? (draft == _draftVersion ? "世界书已保存；下一轮对话读取新版本" : "保存完成；已保留继续编辑的草稿")
                : "保存失败：" + (result?.Error ?? "后台任务失败");
            AISettingsHost.QueueRefresh();
        }

        private static void AddEntry()
        {
            if (_document == null || _document.Entries.Count >= StructuredWorldBook.MaxEntries) return;
            _selected = StructuredWorldBook.NewEntry(_categoryFilter == StructuredWorldBook.ReservedAllCategory
                ? "未分类" : _categoryFilter);
            _document.Entries.Add(_selected);
            unchecked { _draftVersion++; }
            _status = "已添加条目，保存后生效";
            AISettingsHost.QueueRefresh();
        }

        private static void DeleteEntry()
        {
            if (_document == null || _selected == null) return;
            int index = _document.Entries.IndexOf(_selected);
            if (index < 0) return;
            _document.Entries.RemoveAt(index);
            _selected = _document.Entries.FirstOrDefault();
            unchecked { _draftVersion++; }
            _status = "已删除条目，保存后生效";
            AISettingsHost.QueueRefresh();
        }

        internal static void ResetToDefault()
        {
            if (_busy || !IsAvailable) return;
            if (!LongTextEditorOperationGate.TryAcquire(OperationOwner, out long token)) return;
            _busy = true;
            _operationToken = token;
            AISettingsHost.QueueRefresh();
            int generation = WorldLifecycle.Generation;
            uint worldId = WorldLifecycle.WorldId;
            int taiwuId = _taiwuId;
            string directory = JianghuYoulingPaths.Personas;
            _status = "正在还原默认世界书……";
            Task<OperationResult> task = Task.Run(() => ResetDocument(directory, taiwuId,
                generation, worldId));
            ConfigHost.Instance.StartCoroutine(WaitReset(task, token, generation, worldId));
        }

        private static OperationResult ResetDocument(string directory, int taiwuId,
            int generation, uint worldId)
        {
            return WorldBookIoCoordinator.Run(worldId, generation, () =>
            {
                SettingsBatchTransaction transaction = null;
                try
                {
                    transaction = SettingsBatchTransaction.Capture(new[]
                    {
                        Path.Combine(directory, "Worldbook_" + taiwuId + ".txt"),
                        StructuredWorldBookStore.LayoutPath(directory, taiwuId)
                    });
                    if (!WorldBookStore.ClearFromDirectory(directory, taiwuId)
                        || !StructuredWorldBookStore.InvalidateFromDirectory(directory, taiwuId))
                    {
                        transaction.Rollback();
                        transaction.Dispose();
                        return new OperationResult { Error = "默认重置写入失败" };
                    }
                    var snapshot = StructuredWorldBookStore.LoadFromDirectory(directory, taiwuId);
                    if (snapshot?.Document == null)
                    {
                        transaction.Rollback();
                        transaction.Dispose();
                        return new OperationResult { Error = "默认世界书读回失败" };
                    }
                    transaction.Commit();
                    transaction.Dispose();
                    return new OperationResult { Document = snapshot.Document };
                }
                catch (Exception ex)
                {
                    transaction?.Rollback();
                    transaction?.Dispose();
                    return new OperationResult { Error = ex.GetType().Name };
                }
            });
        }

        private static IEnumerator WaitReset(Task<OperationResult> task, long token, int generation, uint worldId)
        {
            while (task != null && !task.IsCompleted) yield return null;
            OperationResult result = task != null && !task.IsFaulted ? task.Result : null;
            bool valid = _operationToken == token && WorldLifecycle.IsSameWorld(generation)
                && WorldLifecycle.WorldId == worldId;
            if (!valid)
            {
                FinishOperation(token);
                yield break;
            }
            FinishOperation(token);
            _document = result?.Document;
            _selected = _document?.Entries.FirstOrDefault();
            MarkDraft();
            _status = _document == null ? "还原失败" : "已还原默认世界书";
            AISettingsHost.QueueRefresh();
        }

        private static void Import()
        {
            if (_busy || !IsAvailable) return;
            if (string.IsNullOrWhiteSpace(_importPath))
            {
                _status = "请先填写世界书 JSON 路径";
                AISettingsHost.QueueRefresh();
                return;
            }
            if (!LongTextEditorOperationGate.TryAcquire(OperationOwner, out long token)) return;
            _busy = true;
            _operationToken = token;
            AISettingsHost.QueueRefresh();
            int generation = WorldLifecycle.Generation;
            uint worldId = WorldLifecycle.WorldId;
            int taiwuId = _taiwuId;
            string directory = JianghuYoulingPaths.Personas;
            string path = _importPath.Trim();
            _status = "正在导入并保存世界书……";
            Task<OperationResult> task = Task.Run(() => ImportDocument(directory, taiwuId, path,
                generation, worldId));
            ConfigHost.Instance.StartCoroutine(WaitImport(task, token, generation, worldId));
        }

        private static OperationResult ImportDocument(string directory, int taiwuId, string path,
            int generation, uint worldId)
        {
            return WorldBookIoCoordinator.Run(worldId, generation, () =>
            {
                SettingsBatchTransaction transaction = null;
                try
                {
                    var info = new FileInfo(Path.GetFullPath(path));
                    if (!info.Exists || info.Length <= 1 || info.Length > StructuredWorldBookExchange.MaxExchangeBytes)
                        return new OperationResult { Error = "导入文件不存在或大小无效" };
                    string raw = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(info.FullName));
                    if (!StructuredWorldBookExchange.TryImport(raw, out var imported, out string error))
                        return new OperationResult { Error = error };
                    if (StructuredWorldBook.Compile(imported.Document).Length > WorldBookStore.MaxCustomWorldBookChars)
                        return new OperationResult { Error = "导入后的世界书超过长度上限" };
                    transaction = SettingsBatchTransaction.Capture(new[]
                    {
                        Path.Combine(directory, "Worldbook_" + taiwuId + ".txt"),
                        StructuredWorldBookStore.LayoutPath(directory, taiwuId)
                    });
                    if (!StructuredWorldBookStore.SaveFromDirectory(directory, taiwuId, imported.Document, out error))
                    {
                        transaction.Rollback();
                        transaction.Dispose();
                        return new OperationResult { Error = error };
                    }
                    transaction.Commit();
                    transaction.Dispose();
                    return new OperationResult { Document = imported.Document };
                }
                catch (Exception ex)
                {
                    transaction?.Rollback();
                    transaction?.Dispose();
                    return new OperationResult { Error = ex.GetType().Name };
                }
            });
        }

        private static IEnumerator WaitImport(Task<OperationResult> task, long token, int generation, uint worldId)
        {
            while (task != null && !task.IsCompleted) yield return null;
            OperationResult result = task != null && !task.IsFaulted ? task.Result : null;
            bool valid = _operationToken == token && WorldLifecycle.IsSameWorld(generation)
                && WorldLifecycle.WorldId == worldId;
            if (!valid)
            {
                FinishOperation(token);
                yield break;
            }
            FinishOperation(token);
            _document = result?.Document;
            _selected = _document?.Entries.FirstOrDefault();
            MarkDraft();
            _status = _document == null ? "导入失败：" + (result?.Error ?? "后台任务失败") : "世界书已导入并保存";
            AISettingsHost.QueueRefresh();
        }

        private static void Export()
        {
            if (_busy || _document == null || !IsAvailable) return;
            if (!StructuredWorldBookExchange.TryExport(CloneDocument(_document), out string json, out string error))
            {
                _status = "导出失败：" + error;
                AISettingsHost.QueueRefresh();
                return;
            }
            try
            {
                string directory = ExchangeDirectory();
                string path = Path.Combine(directory, "江湖有灵世界书-" + _taiwuId + "-"
                    + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".json");
                File.WriteAllText(path, json, new UTF8Encoding(false));
                GUIUtility.systemCopyBuffer = path;
                _status = "已导出世界书，路径已复制：" + path;
            }
            catch (Exception ex) { _status = "导出失败：" + ex.GetType().Name; }
            AISettingsHost.QueueRefresh();
        }

        private static string ExchangeDirectory()
        {
            string path = Path.Combine(JianghuYoulingPaths.Exports, "WorldBooks");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void OpenExchangeDirectory()
        {
            try
            {
                Application.OpenURL(new Uri(ExchangeDirectory()).AbsoluteUri);
                _status = "已打开世界书交换目录";
            }
            catch (Exception ex) { _status = "打开交换目录失败：" + ex.GetType().Name; }
            AISettingsHost.QueueRefresh();
        }

        private static StructuredWorldBook.Document CloneDocument(StructuredWorldBook.Document source)
        {
            var clone = new StructuredWorldBook.Document
            {
                Version = source.Version,
                DefaultTemplateRevision = source.DefaultTemplateRevision,
                SourceFingerprint = source.SourceFingerprint,
                Categories = new List<string>(source.Categories),
                LegacyFinalInstructions = new List<string>(source.LegacyFinalInstructions)
            };
            foreach (var entry in source.Entries)
                clone.Entries.Add(new StructuredWorldBook.Entry
                {
                    Id = entry.Id, Category = entry.Category, Name = entry.Name, Enabled = entry.Enabled,
                    Mode = entry.Mode, Priority = entry.Priority, Keywords = entry.Keywords, Content = entry.Content
                });
            return clone;
        }

        private static void FinishOperation(long token)
        {
            LongTextEditorOperationGate.Release(OperationOwner, token);
            if (_operationToken == token)
            {
                _operationToken = 0;
                _busy = false;
            }
        }

        internal static void ResetForWorldExit()
        {
            WorldBookIoCoordinator.Invalidate(() =>
            {
                unchecked { _contextVersion++; }
                _taiwuId = 0;
                _document = null;
                _selected = null;
                _status = "请先进入存档，再编辑世界书";
                if (_busy)
                {
                    _operationToken = 0;
                    _busy = false;
                }
            });
        }

        private sealed class OperationResult
        {
            public StructuredWorldBook.Document Document;
            public string Error;
        }
    }
}
