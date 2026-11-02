using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JianghuYouling.DevTest
{
    internal static class AISettingsWorldBookTests
    {
        internal static void Run()
        {
            string pane = ReadRepoFile("src/JianghuYouling.Frontend/UI/AISettingsWorldBookPane.cs");
            string editor = ReadRepoFile("src/JianghuYouling.Frontend/UI/AISettingsWorldBookEditor.cs");
            Check(pane.Contains("CreateInfos()") && !pane.Contains("StructuredWorldBookWindow.Open"),
                "world book uses native settings instead of the legacy window");
            Check(editor.Contains("WorldLifecycle.HasWorldIdentity")
                && editor.Contains("请先进入存档"), "world book requires a live save context");
            Check(pane.Contains("SetInteractable(AISettingsWorldBookEditor.IsAvailable)"),
                "world book controls are disabled outside a save");
            Check(editor.Contains("Task.Run") && editor.Contains("WorldLifecycle.IsSameWorld")
                && editor.Contains("WorldLifecycle.WorldId == worldId"),
                "world book loads and saves reject stale world callbacks");
            Check(editor.Contains("Category") && editor.Contains("Enabled") && editor.Contains("Mode")
                && editor.Contains("Priority") && editor.Contains("Keywords") && editor.Contains("Content"),
                "world book exposes every structured entry field");
            Check(editor.Contains("AddEntry") && editor.Contains("DeleteEntry")
                && editor.Contains("Save") && editor.Contains("ResetToDefault")
                && editor.Contains("Import") && editor.Contains("Export"),
                "world book preserves add, delete, save, reset, import and export");
            Check(editor.Contains("SettingsBatchTransaction.Capture")
                && editor.Contains("transaction.Rollback()"),
                "world book saving rolls back failed transactions");
            string context = Section(editor, "private static void EnsureContext()",
                "private static int ResolveTaiwuId()");
            int identityGuard = context.IndexOf("if (!WorldLifecycle.HasWorldIdentity", StringComparison.Ordinal);
            int gameDataRead = context.IndexOf("int taiwuId = ResolveTaiwuId();", StringComparison.Ordinal);
            Check(identityGuard >= 0 && (gameDataRead < 0 || identityGuard < gameDataRead),
                "world identity is checked before any game-data lookup");
            Check(editor.Contains("DynamicText[(ushort)EmptyText] = \"暂无条目\"")
                && editor.Contains("() => _selected?.Content ?? \"\""),
                "main-menu world book keeps empty entry and content placeholders");
            TestWorldIoCoordinator();
            Console.WriteLine("[PASS] AI settings world book native editor and world gate");
        }

        private static void TestWorldIoCoordinator()
        {
            uint oldWorld = JianghuYoulingPaths.CurrentWorldId;
            int oldGeneration = WorldLifecycle.Generation;
            try
            {
                JianghuYoulingPaths.CurrentWorldId = 1;
                WorldLifecycle.Generation = 1;
                bool called = false;
                bool rejected = false;
                try
                {
                    WorldBookIoCoordinator.Run(1, 2, () => { called = true; return true; });
                }
                catch (InvalidOperationException) { rejected = true; }
                Check(rejected && !called, "stale world writes are rejected before mutation");

                bool entered = false, release = false;
                Task write = Task.Run(() => WorldBookIoCoordinator.Run(1, 1, () =>
                {
                    entered = true;
                    while (!release) Thread.Sleep(1);
                    return true;
                }));
                while (!entered) Thread.Sleep(1);
                Task invalidate = Task.Run(() => WorldBookIoCoordinator.Invalidate(() =>
                {
                    WorldLifecycle.Generation = 2;
                    JianghuYoulingPaths.CurrentWorldId = 2;
                }));
                Thread.Sleep(20);
                Check(!invalidate.IsCompleted, "world transition waits for the active transaction");
                release = true;
                Task.WaitAll(write, invalidate);
                Check(invalidate.IsCompleted, "world transition resumes after transaction completion");
            }
            finally
            {
                JianghuYoulingPaths.CurrentWorldId = oldWorld;
                WorldLifecycle.Generation = oldGeneration;
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

        private static string Section(string source, string start, string end)
        {
            int from = source.IndexOf(start, StringComparison.Ordinal);
            int to = from < 0 ? -1 : source.IndexOf(end, from + start.Length, StringComparison.Ordinal);
            Check(from >= 0 && to > from, "source section: " + start);
            return source.Substring(from, to - from);
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("AI settings world book: " + label);
        }
    }
}
