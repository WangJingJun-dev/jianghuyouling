using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Llm;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JianghuYouling
{
    internal static class AISettingsModelPane
    {
        internal const LanguageKey BaseUrlTitle = (LanguageKey)6170;
        internal const LanguageKey ApiKeyTitle = (LanguageKey)6171;
        internal const LanguageKey ModelTitle = (LanguageKey)6172;
        internal const LanguageKey BgModelTitle = (LanguageKey)6173;
        internal const LanguageKey MaxTokensTitle = (LanguageKey)6174;
        internal const LanguageKey ContextWindowTitle = (LanguageKey)6175;
        internal const LanguageKey TestTitle = (LanguageKey)6176;
        internal const LanguageKey TestActionText = (LanguageKey)6177;
        internal const LanguageKey TestStatusTitle = (LanguageKey)6178;
        private const string DefaultBaseUrl = "https://api.deepseek.com";
        private const string DefaultChatPath = "/chat/completions";
        private const string DefaultModel = "deepseek-v4-pro";
        private const string DefaultBgModel = "deepseek-v4-flash";
        private const string DefaultMaxTokens = "32768";
        private const string DefaultContextWindow = "1000000";
        private static CancellationTokenSource _testCancellation;
        private static long _testVersion;
        internal static string TestStatus { get; private set; } = string.Empty;

        internal static List<ISettingItemInfo> CreateInfos()
        {
            var infos = AISettingsModelProfileView.CreateInfos();
            ESettingSubCategory sub = AISettingsCategorySync.ModelSubCategory;
            infos.Add(new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 10, BaseUrlTitle)
                { CharacterLimit = 2048 }, () => Value("baseUrl", DefaultBaseUrl),
                value => SaveField("baseUrl", value)));
            infos.Add(new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 11, ApiKeyTitle)
                { CharacterLimit = 4096, Password = true }, () => Value("apiKey", ""),
                value => SaveField("apiKey", value)));
            infos.Add(new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 12, ModelTitle)
                { CharacterLimit = 256 }, () => Value("model", DefaultModel),
                value => SaveField("model", value)));
            infos.Add(new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 13, BgModelTitle)
                { CharacterLimit = 256 }, () => Value("bgModel", DefaultBgModel),
                value => SaveField("bgModel", value)));
            infos.Add(new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 14, MaxTokensTitle)
                { CharacterLimit = 16 }, () => Value("maxTokens", DefaultMaxTokens),
                value => SaveField("maxTokens", value)));
            infos.Add(new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 15, ContextWindowTitle)
                { CharacterLimit = 16 }, () => Value("contextWindow", DefaultContextWindow),
                value => SaveField("contextWindow", value)));
            infos.Add(new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 16, TestTitle)
                { ActionText = TestActionText }, () => 0, _ => StartTest()));
            return infos;
        }

        private static JObject CurrentConfig()
        {
            JObject config = LlmService.ReadRaw() ?? new JObject();
            if (config["baseUrl"] == null) config["baseUrl"] = DefaultBaseUrl;
            if (config["chatPath"] == null) config["chatPath"] = DefaultChatPath;
            if (config["model"] == null) config["model"] = DefaultModel;
            if (config["bgModel"] == null) config["bgModel"] = DefaultBgModel;
            if (config["maxTokens"] == null) config["maxTokens"] = DefaultMaxTokens;
            if (config["contextWindow"] == null) config["contextWindow"] = DefaultContextWindow;
            return config;
        }

        private static string Value(string key, string fallback)
        {
            JObject config = CurrentConfig();
            if (key == "maxTokens")
            {
                string configured = LlmService.ReadConfiguredMaxTokens(config);
                return string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
            }
            if (key == "contextWindow")
            {
                string configured = LlmService.ReadConfiguredContextWindow(config);
                return string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
            }
            string value = config[key]?.ToString();
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static void SaveField(string field, string value)
        {
            JObject config = CurrentConfig();
            config[field] = (value ?? "").Trim();
            if (field == "maxTokens") config[LlmService.MaxTokensDefaultVersionField] = LlmService.MaxTokensDefaultVersion;
            if (field == "contextWindow") config[LlmService.ContextWindowDefaultVersionField] = LlmService.ContextWindowDefaultVersion;
            string apiKey = config["apiKey"]?.ToString() ?? "";
            config.Remove("apiKey");
            if (!ValidateConfig(config, apiKey, out string error))
                throw new InvalidOperationException("model_validation_failed");
            if (!LlmService.SaveRaw(config, apiKey, out error))
                throw new InvalidOperationException("model_save_failed");
        }

        internal static JObject BuildCurrentConfig(out string apiKey, out string error)
        {
            JObject config = CurrentConfig();
            apiKey = config["apiKey"]?.ToString() ?? "";
            config.Remove("apiKey");
            return ValidateConfig(config, apiKey, out error) ? config : null;
        }

        internal static bool ValidateConfig(JObject config, string apiKey, out string error)
        {
            error = null;
            string baseUrl = (config?["baseUrl"]?.ToString() ?? "").Trim();
            string model = (config?["model"]?.ToString() ?? "").Trim();
            string maxTokensText = LlmService.ReadConfiguredMaxTokens(config);
            string contextWindowText = LlmService.ReadConfiguredContextWindow(config);
            if (baseUrl.Length == 0 || model.Length == 0) { error = "baseUrl_or_model_empty"; return false; }
            if (!int.TryParse((maxTokensText ?? "").Trim(), out int maxTokens) || maxTokens < 0)
            { error = "max_tokens_invalid"; return false; }
            int contextWindow = OpenAiCompatibleClient.DefaultContextWindowTokens;
            if (!string.IsNullOrWhiteSpace(contextWindowText)
                && (!int.TryParse(contextWindowText.Trim(), out contextWindow) || contextWindow < 8192))
            { error = "context_window_invalid"; return false; }
            if (maxTokens > 0 && maxTokens + 1024 >= contextWindow)
            { error = "context_window_too_small"; return false; }
            var candidate = new OpenAiCompatibleClient(baseUrl, DefaultChatPath, (apiKey ?? "").Trim(), model);
            if (!string.IsNullOrWhiteSpace(candidate.ConfigurationError))
            { error = candidate.ConfigurationError; return false; }
            return true;
        }

        private static void StartTest()
        {
            if (_testCancellation != null) return;
            JObject config = CurrentConfig();
            string baseUrl = config["baseUrl"]?.ToString() ?? "";
            string model = config["model"]?.ToString() ?? "";
            string apiKey = config["apiKey"]?.ToString() ?? "";
            var candidate = new OpenAiCompatibleClient(baseUrl, DefaultChatPath, apiKey, model);
            if (!string.IsNullOrWhiteSpace(candidate.ConfigurationError) || ConfigHost.Instance == null)
            {
                TestStatus = "测试失败：模型配置无效或界面宿主缺失";
                AISettingsHost.QueueRefresh();
                return;
            }
            var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(45));
            _testCancellation = cancellation;
            long version = ++_testVersion;
            TestStatus = "测试中……";
            AISettingsHost.QueueRefresh();
            ConfigHost.Instance.StartCoroutine(TestCo(baseUrl, apiKey, model, version, cancellation));
        }

        private static IEnumerator TestCo(string baseUrl, string apiKey, string model, long version,
            CancellationTokenSource cancellation)
        {
            var client = new OpenAiCompatibleClient(baseUrl, DefaultChatPath, apiKey, model);
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("这是接口连通性测试。只回复 pong，不要解释。"),
                LlmMessage.User("ping")
            };
            var stopwatch = Stopwatch.StartNew();
            Task<LlmResult> task = client.SendAsync(messages, maxTokens: 256, tag: "连接测试",
                ct: cancellation.Token, reasoningPolicy: LlmReasoningPolicy.Off);
            while (!task.IsCompleted && version == _testVersion
                && ReferenceEquals(_testCancellation, cancellation)) yield return null;
            if (version != _testVersion || !ReferenceEquals(_testCancellation, cancellation)) yield break;
            stopwatch.Stop();
            try
            {
                LlmResult result = task.Result;
                TestStatus = result != null && result.Ok
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

        internal static void ResetToDefault()
        {
            CancelActiveTest();
            var config = new JObject
            {
                ["baseUrl"] = DefaultBaseUrl,
                ["chatPath"] = DefaultChatPath,
                ["model"] = DefaultModel,
                ["bgModel"] = DefaultBgModel,
                ["maxTokens"] = DefaultMaxTokens,
                [LlmService.MaxTokensDefaultVersionField] = LlmService.MaxTokensDefaultVersion,
                ["contextWindow"] = DefaultContextWindow,
                [LlmService.ContextWindowDefaultVersionField] = LlmService.ContextWindowDefaultVersion
            };
            TestStatus = LlmService.SaveRaw(config, "", out string error)
                ? "已还原默认模型配置" : "还原失败：" + (error ?? "配置写入失败");
            AISettingsHost.QueueRefresh();
        }

        internal static void CancelActiveTest()
        {
            unchecked { _testVersion++; }
            try { _testCancellation?.Cancel(); } catch { }
            _testCancellation = null;
        }
    }
}
