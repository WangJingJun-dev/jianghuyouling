using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Web;

namespace JianghuYouling
{
    internal static class AISettingsVoicePane
    {
        internal const LanguageKey BaseUrlTitle = (LanguageKey)6180;
        internal const LanguageKey ApiKeyTitle = (LanguageKey)6181;
        internal const LanguageKey ModelTitle = (LanguageKey)6182;
        internal const LanguageKey DialogueTitle = (LanguageKey)6183;
        internal const LanguageKey FullText = (LanguageKey)6184;
        internal const LanguageKey DialogueOnlyText = (LanguageKey)6185;
        internal const LanguageKey EnabledTitle = (LanguageKey)6360;
        internal const LanguageKey TestTitle = (LanguageKey)6361;
        internal const LanguageKey TestActionText = (LanguageKey)6362;
        internal const LanguageKey TestStatusTitle = (LanguageKey)6363;
        private const string DefaultBaseUrl = "https://openspeech.bytedance.com/api/v3/tts/unidirectional";
        private const string DefaultModel = "seed-tts-2.0";
        private static CancellationTokenSource _testCancellation;
        private static long _testVersion;
        internal static string TestStatus { get; private set; } = string.Empty;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            ESettingSubCategory sub = AISettingsCategorySync.VoiceSubCategory;
            return new List<ISettingItemInfo>
            {
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 1, EnabledTitle),
                    () => TtsConfig.IsEnabled, value => Require(SaveField("enabled", value ? "1" : "0"))),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 2, BaseUrlTitle)
                    { CharacterLimit = 2048 }, () => LoadValues(out _, out _).baseUrl,
                    value => Require(SaveField("baseUrl", value))),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 3, ApiKeyTitle)
                    { CharacterLimit = 4096, Password = true }, () => LoadValues(out _, out _).apiKey,
                    value => Require(SaveField("apiKey", value))),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 4, ModelTitle)
                    { CharacterLimit = 256 }, () => LoadValues(out _, out _).model,
                    value => Require(SaveField("model", value))),
                new SettingItemInfo<sbyte>(new SwitchButtonSettingAttribute(sub, 5, DialogueTitle)
                    { Options = new[] { FullText, DialogueOnlyText } },
                    () => (sbyte)(LoadValues(out _, out _).dialogueOnly ? 1 : 0),
                    value => Require(SaveField("dialogueOnly", value == 1 ? "1" : "0"))),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 6, TestTitle)
                    { ActionText = TestActionText }, () => 0, _ => StartTest())
            };
        }

        private static (string provider, string baseUrl, string apiKey, string model, bool dialogueOnly, bool enabled)
            LoadValues(out bool legacyDefault, out bool crossProviderKey)
        {
            var raw = TtsConfig.LoadRaw();
            TtsSettings settings = TtsSettings.Load();
            crossProviderKey = IsLegacyMiniMaxDefault(raw.provider, raw.baseUrl, settings.Model);
            legacyDefault = crossProviderKey || IsLegacySeedAudioDefault(raw.provider, raw.baseUrl, settings.Model);
            string baseUrl = legacyDefault || string.IsNullOrWhiteSpace(raw.baseUrl)
                ? DefaultBaseUrl : raw.baseUrl;
            string apiKey = crossProviderKey ? "" : raw.apiKey ?? "";
            string model = legacyDefault || string.IsNullOrWhiteSpace(settings.Model)
                ? DefaultModel : settings.Model;
            return (raw.provider, baseUrl, apiKey, model, settings.DialogueOnly, TtsConfig.IsEnabled);
        }

        private static bool SaveField(string field, string value)
        {
            var current = LoadValues(out bool legacyDefault, out _);
            if (field == "enabled") current.enabled = value == "1";
            if (field == "baseUrl") current.baseUrl = (value ?? "").Trim();
            if (field == "apiKey") current.apiKey = (value ?? "").Trim();
            if (field == "model") current.model = (value ?? "").Trim();
            if (field == "dialogueOnly") current.dialogueOnly = value == "1";
            SettingsBatchTransaction transaction = null;
            try
            {
                transaction = SettingsBatchTransaction.Capture(new[]
                {
                    Path.Combine(JianghuYoulingPaths.Settings, "tts.json"),
                    Path.Combine(JianghuYoulingPaths.Settings, "tts_params.json")
                });
                if (!TtsConfig.SaveExplicit("", current.baseUrl, current.apiKey, current.model, current.enabled))
                {
                    if (field == "enabled") TestStatus = "开关保存失败：" + TtsConfig.LastSaveError;
                    transaction.Rollback();
                    AISettingsHost.QueueRefresh();
                    return false;
                }
                TtsSettings settings = TtsSettings.Load();
                if (legacyDefault)
                {
                    settings.Voice = "";
                    settings.Emotion = "";
                }
                settings.Model = current.model;
                settings.DialogueOnly = current.dialogueOnly;
                if (!settings.Save())
                {
                    if (field == "enabled") TestStatus = "开关保存失败：语音参数写入失败";
                    transaction.Rollback();
                    AISettingsHost.QueueRefresh();
                    return false;
                }
                transaction.Commit();
                if (field == "enabled")
                {
                    if (!current.enabled) CancelActiveTest();
                    TestStatus = current.enabled ? "语音功能已开启，可测试接口" : "语音功能已关闭";
                    ChatWindow.NotifyMediaAvailabilityChanged();
                    AISettingsHost.QueueRefresh();
                }
                return true;
            }
            catch
            {
                transaction?.Rollback();
                return false;
            }
            finally { transaction?.Dispose(); }
        }

        private static void StartTest()
        {
            if (_testCancellation != null) return;
            var current = LoadValues(out _, out _);
            if (!current.enabled || string.IsNullOrWhiteSpace(current.apiKey))
            {
                TestStatus = !current.enabled ? "测试失败：请先开启语音功能" : "测试失败：请填写语音 API Key";
                AISettingsHost.QueueRefresh();
                return;
            }
            if (ConfigHost.Instance == null)
            {
                TestStatus = "测试失败：界面宿主缺失";
                AISettingsHost.QueueRefresh();
                return;
            }
            var resolved = TtsConfig.Resolve();
            if (string.IsNullOrWhiteSpace(resolved.baseUrl))
            {
                TestStatus = "测试失败：语音配置无效";
                AISettingsHost.QueueRefresh();
                return;
            }
            TtsSettings settings = TtsSettings.Load();
            string model = string.IsNullOrWhiteSpace(settings.Model) ? resolved.model : settings.Model;
            bool seedTts2 = resolved.provider == TtsProviderUtil.ProviderVolcengineSeedAudio
                && resolved.baseUrl.IndexOf("/api/v3/tts/unidirectional", StringComparison.OrdinalIgnoreCase) >= 0;
            string voice = string.IsNullOrWhiteSpace(settings.Voice)
                ? TtsConfig.PickVoice(resolved.provider, true, 0, 20, "") : settings.Voice;
            if (seedTts2 && !voice.StartsWith("zh_", StringComparison.OrdinalIgnoreCase))
                voice = TtsConfig.PickVoice(resolved.provider, true, 0, 20, "");
            string profile = TtsConfig.PerformanceProfile(resolved.provider, true, 0, 20, "");
            var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(60));
            _testCancellation = cancellation;
            long version = ++_testVersion;
            TestStatus = "测试中……";
            AISettingsHost.QueueRefresh();
            ConfigHost.Instance.StartCoroutine(TestCo(resolved.provider, resolved.baseUrl, resolved.apiKey,
                model, voice, profile, seedTts2, version, cancellation));
        }

        private static IEnumerator TestCo(string provider, string baseUrl, string apiKey, string model,
            string voice, string profile, bool seedTts2, long version,
            CancellationTokenSource cancellation)
        {
            var stopwatch = Stopwatch.StartNew();
            Task<MiniMaxAudioResult> task = VoicePlayer.DispatchSynthesis(provider, baseUrl, apiKey, model,
                "江湖有灵，语音接口测试。", voice, profile, 1f, "neutral", 1f, 1f, 0, seedTts2,
                cancellation.Token);
            while (!task.IsCompleted && version == _testVersion
                && ReferenceEquals(_testCancellation, cancellation)) yield return null;
            if (version != _testVersion || !ReferenceEquals(_testCancellation, cancellation)) yield break;
            stopwatch.Stop();
            try
            {
                MiniMaxAudioResult result = task.Result;
                TestStatus = result != null && result.Ok && result.Mp3 != null
                    ? "测试成功（" + stopwatch.ElapsedMilliseconds + " ms）"
                    : "测试失败：" + (result?.Error ?? "未知错误");
            }
            catch (Exception ex)
            {
                TestStatus = "测试失败：" + ((ex as AggregateException)?.GetBaseException()?.GetType().Name
                    ?? ex.GetType().Name);
            }
            _testCancellation = null;
            try { cancellation.Dispose(); } catch { }
            AISettingsHost.QueueRefresh();
        }

        internal static void CancelActiveTest()
        {
            unchecked { _testVersion++; }
            try { _testCancellation?.Cancel(); } catch { }
            _testCancellation = null;
        }

        internal static void ResetToDefault()
        {
            CancelActiveTest();
            SettingsBatchTransaction transaction = null;
            try
            {
                transaction = SettingsBatchTransaction.Capture(new[]
                {
                    Path.Combine(JianghuYoulingPaths.Settings, "tts.json"),
                    Path.Combine(JianghuYoulingPaths.Settings, "tts_params.json")
                });
                bool saved = TtsConfig.SaveExplicit("", DefaultBaseUrl, "", DefaultModel, false);
                if (saved) saved = new TtsSettings { Model = DefaultModel }.Save();
                if (saved) transaction.Commit();
                else transaction.Rollback();
                TestStatus = saved ? "已还原默认语音配置，功能保持关闭" : "语音配置还原失败";
            }
            catch
            {
                transaction?.Rollback();
                TestStatus = "语音配置还原失败";
            }
            finally { transaction?.Dispose(); }
            AISettingsHost.QueueRefresh();
        }

        private static bool IsLegacyMiniMaxDefault(string provider, string baseUrl, string model)
        {
            string p = (provider ?? "").Trim().ToLowerInvariant();
            string b = (baseUrl ?? "").Trim().TrimEnd('/').ToLowerInvariant();
            string m = (model ?? "").Trim().ToLowerInvariant();
            if (p.Length > 0 && p != TtsProviderUtil.ProviderMiniMax) return false;
            return (b == "https://api.minimaxi.com/v1" || b == "https://api.minimax.com/v1")
                && m == "speech-02-turbo";
        }

        private static bool IsLegacySeedAudioDefault(string provider, string baseUrl, string model)
        {
            string p = (provider ?? "").Trim().ToLowerInvariant();
            string b = (baseUrl ?? "").Trim().TrimEnd('/').ToLowerInvariant();
            string m = (model ?? "").Trim().ToLowerInvariant();
            return (p.Length == 0 || p == TtsProviderUtil.ProviderVolcengineSeedAudio)
                && b == "https://openspeech.bytedance.com/api/v3/tts/create"
                && m == "seed-audio-1.0";
        }

        private static void Require(bool saved)
        {
            if (!saved) throw new InvalidOperationException("voice_save_failed");
        }
    }
}
