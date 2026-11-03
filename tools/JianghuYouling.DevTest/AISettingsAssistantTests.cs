using System;
using System.IO;
using System.Text;
using JianghuYouling.Core.Persona;

namespace JianghuYouling.DevTest
{
    internal static class AISettingsAssistantTests
    {
        internal static void Run()
        {
            string oldRoot = JianghuYoulingPaths.Root;
            string root = Path.Combine(Path.GetTempPath(), "JHYL_AISettingsAssistant_" + Guid.NewGuid().ToString("N"));
            try
            {
                JianghuYoulingPaths.Root = root;
                Directory.CreateDirectory(JianghuYoulingPaths.Settings);
                string defaultPersona = SpecialPersonaCatalog.AssistantPersona;
                Check(!string.IsNullOrWhiteSpace(defaultPersona), "built-in assistant persona is visible");
                Check(AssistantPersonaStore.LoadDisplay() == defaultPersona,
                    "empty custom persona displays the complete built-in default");
                Check(AssistantPersonaStore.SaveDisplay(defaultPersona), "unchanged default saves successfully");
                Check(AssistantPersonaStore.Load().Length == 0,
                    "saving the unchanged default does not create a custom persona");
                Check(AssistantPersonaStore.SaveDisplay("测试自定义性格"), "custom persona saves successfully");
                Check(AssistantPersonaStore.Load() == "测试自定义性格", "custom persona persists");
                Check(AssistantPersonaStore.LoadDisplay() == "测试自定义性格", "custom persona takes display priority");
                Check(AssistantPersonaStore.SaveDisplay(""), "clearing the custom persona succeeds");
                Check(AssistantPersonaStore.Load().Length == 0, "clearing leaves no custom persona");
                Check(AssistantPersonaStore.LoadDisplay() == defaultPersona, "clearing restores the built-in default");

                string source = ReadRepoFile("src/JianghuYouling.Frontend/UI/AISettingsAssistantPane.cs")
                    + ReadRepoFile("src/JianghuYouling.Frontend/UI/AISettingsHost.cs");
                Check(source.Contains("助手名字") && source.Contains("助手形象图") && source.Contains("助手人设"),
                    "assistant name, face and persona are independent native parameters");
                Check(source.Contains("NativeTextSettingAttribute"),
                    "assistant fields use native controls");
                string model = ReadRepoFile("src/JianghuYouling.Frontend/UI/AISettingsModelPane.cs");
                Check(model.Contains("baseUrl") && model.Contains("apiKey") && model.Contains("bgModel")
                    && model.Contains("maxTokens") && model.Contains("contextWindow"),
                    "model pane covers every connection and budget field");
                Check(model.Contains("Password = true") && model.Contains("LlmService.SaveRaw"),
                    "model key stays masked and uses the protected save path");
                Check(model.Contains("NativeActionSettingAttribute"), "model pane uses native action controls");
                string profiles = ReadRepoFile("src/JianghuYouling.Frontend/UI/AISettingsModelProfileView.cs");
                Check(profiles.Contains("LlmProfileStore.Save") && profiles.Contains("LlmProfileStore.TryLoad")
                    && profiles.Contains("LlmProfileStore.Delete"), "profile save, activate and delete remain available");
                Check(profiles.Contains("SettingsBatchTransaction.Capture") && profiles.Contains("transaction.Rollback()")
                    && profiles.Contains("LlmService.Reload()"), "profile activation rolls back and reloads after failure");
                Console.WriteLine("[PASS] AI settings assistant, model and profile native parameters");
            }
            finally
            {
                JianghuYoulingPaths.Root = oldRoot;
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static string ReadRepoFile(string relative)
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
                for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
                {
                    string path = Path.Combine(directory.FullName, relative);
                    if (File.Exists(path)) return File.ReadAllText(path, Encoding.UTF8);
                }
            throw new FileNotFoundException(relative);
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("AI settings assistant: " + label);
        }
    }
}
