using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Web;

namespace JianghuYouling
{
    internal static class AISettingsImagePane
    {
        internal const LanguageKey ProviderTitle = (LanguageKey)6190;
        internal const LanguageKey EndpointTitle = (LanguageKey)6191;
        internal const LanguageKey ApiKeyTitle = (LanguageKey)6192;
        internal const LanguageKey ModelTitle = (LanguageKey)6193;
        internal const LanguageKey SizeTitle = (LanguageKey)6194;
        internal const LanguageKey WorkflowTitle = (LanguageKey)6195;
        internal const LanguageKey StyleTitle = (LanguageKey)6196;
        internal const LanguageKey WatermarkTitle = (LanguageKey)6197;
        internal const LanguageKey EnabledTitle = (LanguageKey)6364;
        internal const LanguageKey TestTitle = (LanguageKey)6365;
        internal const LanguageKey TestActionText = (LanguageKey)6366;
        internal const LanguageKey TestStatusTitle = (LanguageKey)6367;
        private const string TestReferencePng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9Zl1sAAAAASUVORK5CYII=";
        private static CancellationTokenSource _testCancellation;
        private static long _testVersion;
        internal static string TestStatus { get; private set; } = "尚未测试";

        internal static List<ISettingItemInfo> CreateInfos()
        {
            ESettingSubCategory sub = AISettingsCategorySync.ImageSubCategory;
            return new List<ISettingItemInfo>
            {
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 1, EnabledTitle),
                    () => ImageGenerationConfig.LoadRaw().Enabled,
                    value => SaveField("enabled", value ? "1" : "0")),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 2, ProviderTitle)
                    { CharacterLimit = 64 }, () => ImageGenerationConfig.LoadRaw().Provider,
                    value => SaveField("provider", value)),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 3, EndpointTitle)
                    { CharacterLimit = 2048 }, () => ImageGenerationConfig.LoadRaw().Endpoint,
                    value => SaveField("endpoint", value)),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 4, ApiKeyTitle)
                    { CharacterLimit = 4096, Password = true }, () => ImageGenerationConfig.LoadRaw().ApiKey,
                    value => SaveField("apiKey", value)),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 5, ModelTitle)
                    { CharacterLimit = 256 }, () => ImageGenerationConfig.LoadRaw().Model,
                    value => SaveField("model", value)),
                new SettingItemInfo<string>(new NativeReadonlyTextSettingAttribute(sub, 6, SizeTitle)
                    { Height = 44f },
                    () => ImageGenerationClient.FixedSizeForProvider(ImageGenerationConfig.LoadRaw().Provider),
                    _ => { }),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 7, WorkflowTitle)
                    { CharacterLimit = ImageGenerationConfig.MaxWorkflowPathChars },
                    () => ImageGenerationConfig.LoadRaw().WorkflowPath,
                    value => SaveField("workflowPath", value)),
                new SettingItemInfo<string>(new NativeTextSettingAttribute(sub, 8, StyleTitle)
                    { Multiline = true, CharacterLimit = ImageGenerationConfig.MaxStyleChars },
                    () => ImageGenerationConfig.LoadRaw().Style, value => SaveField("style", value)),
                new SettingItemInfo<bool>(new BoolSettingAttribute(sub, 9, WatermarkTitle),
                    () => ImageGenerationConfig.LoadRaw().Watermark,
                    value => SaveField("watermark", value ? "1" : "0")),
                new SettingItemInfo<sbyte>(new NativeActionSettingAttribute(sub, 10, TestTitle)
                    { ActionText = TestActionText }, () => 0, _ => StartTest()),
                new SettingItemInfo<string>(new NativeReadonlyTextSettingAttribute(sub, 11, TestStatusTitle)
                    { Height = 64f }, () => TestStatus, _ => { })
            };
        }

        private static void SaveField(string field, string value)
        {
            ImageGenerationConfig.Values current = ImageGenerationConfig.LoadRaw();
            if (field == "enabled") current.Enabled = value == "1";
            if (field == "provider") current.Provider = (value ?? "").Trim();
            if (field == "endpoint") current.Endpoint = (value ?? "").Trim();
            if (field == "apiKey") current.ApiKey = (value ?? "").Trim();
            if (field == "model") current.Model = (value ?? "").Trim();
            if (field == "workflowPath") current.WorkflowPath = (value ?? "").Trim();
            if (field == "style") current.Style = (value ?? "").Trim();
            if (field == "watermark") current.Watermark = value == "1";
            current.Size = ImageGenerationClient.FixedSizeForProvider(current.Provider);
            if (!ImageGenerationConfig.SaveExplicit(current))
            {
                if (field == "enabled")
                {
                    TestStatus = "开关保存失败：" + ImageGenerationConfig.LastSaveError;
                    AISettingsHost.QueueRefresh();
                }
                throw new InvalidOperationException("image_save_failed");
            }
            if (field == "enabled")
            {
                if (!current.Enabled) CancelActiveTest();
                TestStatus = current.Enabled ? "生图功能已开启，可测试接口" : "生图功能已关闭";
                AISettingsHost.QueueRefresh();
            }
            if (field == "provider") AISettingsHost.QueueRefresh();
        }

        private static void StartTest()
        {
            if (_testCancellation != null) return;
            ImageGenerationConfig.Values raw = ImageGenerationConfig.LoadRaw();
            if (!raw.Enabled || string.IsNullOrWhiteSpace(raw.ApiKey))
            {
                TestStatus = !raw.Enabled ? "测试失败：请先开启生图功能" : "测试失败：请填写生图 API Key";
                AISettingsHost.QueueRefresh();
                return;
            }
            if (!ImageGenerationConfig.TryResolve(out ImageGenerationConfig.Values config, out string error)
                || ConfigHost.Instance == null)
            {
                TestStatus = "测试失败：" + (error ?? "界面宿主缺失");
                AISettingsHost.QueueRefresh();
                return;
            }
            var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromMinutes(9));
            _testCancellation = cancellation;
            long version = ++_testVersion;
            TestStatus = "测试中……";
            AISettingsHost.QueueRefresh();
            ConfigHost.Instance.StartCoroutine(TestCo(config, version, cancellation));
        }

        private static IEnumerator TestCo(ImageGenerationConfig.Values config, long version,
            CancellationTokenSource cancellation)
        {
            var request = new ImageGenerationRequest
            {
                Provider = config.Provider,
                Endpoint = config.Endpoint,
                ApiKey = config.ApiKey,
                Model = config.Model,
                Prompt = "武侠山水，生图接口连通性测试。",
                ReferencePng = Convert.FromBase64String(TestReferencePng),
                Size = config.Size,
                Watermark = config.Watermark,
                WorkflowJson = config.WorkflowJson
            };
            var stopwatch = Stopwatch.StartNew();
            Task<ImageGenerationResult> task = ImageGenerationClient.GenerateAsync(request, cancellation.Token);
            while (!task.IsCompleted && version == _testVersion
                && ReferenceEquals(_testCancellation, cancellation)) yield return null;
            if (version != _testVersion || !ReferenceEquals(_testCancellation, cancellation)) yield break;
            stopwatch.Stop();
            try
            {
                ImageGenerationResult result = task.Result;
                TestStatus = result != null && result.Ok && result.Bytes != null
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
            TestStatus = ImageGenerationConfig.SaveExplicit(new ImageGenerationConfig.Values())
                ? "已还原默认生图配置，功能保持关闭" : "生图配置还原失败：" + ImageGenerationConfig.LastSaveError;
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
