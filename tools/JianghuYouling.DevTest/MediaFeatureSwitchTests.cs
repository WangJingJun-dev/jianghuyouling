using System;
using System.IO;
using JianghuYouling.Core.Security;
using JianghuYouling.Core.Web;
using Newtonsoft.Json.Linq;

namespace JianghuYouling.DevTest
{
    internal static class MediaFeatureSwitchTests
    {
        internal static void Run()
        {
            VoiceDefaultsOffAndPreservesCredentials();
            ImageDefaultsOffAndRequiresKeyWhenEnabled();
        }

        private static void VoiceDefaultsOffAndPreservesCredentials()
        {
            string previousRoot = JianghuYoulingPaths.Root;
            string root = Path.Combine(Path.GetTempPath(), "jhy-media-voice-" + Guid.NewGuid().ToString("N"));
            try
            {
                JianghuYoulingPaths.Root = root;
                Directory.CreateDirectory(JianghuYoulingPaths.Settings);
                string path = Path.Combine(JianghuYoulingPaths.Settings, "tts.json");
                Check(ProtectedConfigFile.TrySave(path, new JObject
                {
                    ["provider"] = "",
                    ["baseUrl"] = "https://openspeech.bytedance.com/api/v3/tts/unidirectional",
                    ["model"] = "seed-tts-2.0"
                }, "voice-fixture-key", out string error), "voice fixture saves: " + error);

                Check(!TtsConfig.IsEnabled, "legacy voice config with a key still defaults to disabled");
                Check(!TtsConfig.IsConfigured, "disabled voice is not configured for runtime use");
                Check(string.IsNullOrEmpty(TtsConfig.Resolve().apiKey), "disabled voice resolves no credentials");
                Check(TtsConfig.LoadRaw().apiKey == "voice-fixture-key", "disabled voice retains its saved key");
                Check(TtsConfig.SetEnabled(true), "voice with a key can be enabled");
                Check(TtsConfig.IsConfigured && TtsConfig.Resolve().apiKey == "voice-fixture-key",
                    "enabled voice resolves its independent key");
                Check(TtsConfig.SaveExplicit("", "https://openspeech.bytedance.com/api/v3/tts/unidirectional",
                    "voice-fixture-key", "seed-tts-2.0"), "ordinary voice settings save succeeds");
                Check(TtsConfig.IsEnabled, "ordinary voice settings save preserves the enabled state");
                Check(TtsConfig.SetEnabled(false), "voice can be disabled");
                Check(!TtsConfig.IsConfigured, "disabled voice cannot be used");
                Check(TtsConfig.LoadRaw().apiKey == "voice-fixture-key", "disabling voice does not clear its key");
                Check(TtsConfig.SaveExplicit("", "https://openspeech.bytedance.com/api/v3/tts/unidirectional",
                    "", "seed-tts-2.0"), "disabled voice allows saving an empty key");
                Check(!TtsConfig.SetEnabled(true), "voice without a key cannot be enabled");
                Check((TtsConfig.LastSaveError ?? "").Contains("API Key"), "voice enablement explains the missing key");
            }
            finally
            {
                JianghuYoulingPaths.Root = previousRoot;
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void ImageDefaultsOffAndRequiresKeyWhenEnabled()
        {
            string previousRoot = JianghuYoulingPaths.Root;
            string root = Path.Combine(Path.GetTempPath(), "jhy-media-image-" + Guid.NewGuid().ToString("N"));
            try
            {
                JianghuYoulingPaths.Root = root;
                var values = new ImageGenerationConfig.Values
                {
                    Provider = ImageGenerationClient.ProviderDoubao,
                    Endpoint = ImageGenerationConfig.DefaultEndpoint,
                    ApiKey = "image-fixture-key",
                    Model = ImageGenerationConfig.DefaultModel,
                    Size = ImageGenerationConfig.DefaultSize,
                    Style = "水墨武侠"
                };
                Check(ImageGenerationConfig.SaveExplicit(values), "image fixture saves with default disabled state");
                Check(!ImageGenerationConfig.LoadRaw().Enabled, "image defaults to disabled even when a key exists");
                Check(!ImageGenerationConfig.TryResolve(out _, out string disabledError),
                    "disabled image generation fails closed");
                Check((disabledError ?? "").Contains("未启用"), "disabled image generation reports its switch state");
                Check(ImageGenerationConfig.LoadRaw().ApiKey == "image-fixture-key",
                    "disabled image generation retains its key");

                values.Enabled = true;
                Check(ImageGenerationConfig.SaveExplicit(values), "enabled image configuration with a key saves");
                Check(ImageGenerationConfig.TryResolve(out ImageGenerationConfig.Values resolved, out _),
                    "enabled image configuration resolves");
                Check(resolved.ApiKey == "image-fixture-key", "resolved image configuration uses its own key");
                values.Enabled = false;
                Check(ImageGenerationConfig.SaveExplicit(values), "image configuration can be disabled");
                Check(ImageGenerationConfig.LoadRaw().ApiKey == "image-fixture-key",
                    "disabling image generation does not clear its key");
                Check(!ImageGenerationConfig.TryResolve(out _, out _), "disabled image configuration cannot run");

                values.ApiKey = "";
                values.Enabled = true;
                Check(!ImageGenerationConfig.SaveExplicit(values), "enabled image configuration rejects an empty key");
                Check((ImageGenerationConfig.LastSaveError ?? "").Contains("API Key"),
                    "image enablement explains the missing key");
            }
            finally
            {
                JianghuYoulingPaths.Root = previousRoot;
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            Console.WriteLine("[PASS] " + label);
        }
    }

}

namespace JianghuYouling
{
    public static class VoicePlayer
    {
        internal static void InvalidateCacheForConfigurationChange()
        {
        }
    }
}
