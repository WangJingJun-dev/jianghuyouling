# ConfigWindow Removal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完整删除已无生产入口的旧 `ConfigWindow`，同时保留现行系统设置、人设编辑、世界退出清理和有效回归覆盖。

**Architecture:** 先从 `WorldLifecycle` 断开唯一生产调用，再删除旧窗口文件，最后把旧源码断言逐项迁移到当前 `AISettings*`、`PersonaWindow` 和实际 Store。删除只针对旧 UI 实现，不删除任何玩家配置、模型档案、人设或世界书数据。

**Tech Stack:** C#、.NET Framework 4.8、Unity uGUI、Harmony、PowerShell 回归脚本、DevTest。

**Spec:** `docs/superpowers/specs/2026-11-01-native-ai-settings-design.md`，以及本轮确认的「删除旧 `ConfigWindow`」范围。

## Global Constraints

- 唯一开发目录为 `E:\Work\VibeCoding\TaiWuDev\JianghuYouling`；本地运行目录为 `D:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\JianghuYouling`。
- 不提交 Git、不上传工坊、不使用游戏自动化工具；实机由用户手动验证。
- 不删除 `Settings`、模型档案、人设、世界书或任何存档数据。
- 不修改无关既有失败；反编译基线缺失与既有手动总结检查失败应单独报告。
- 当前聊天窗「人设」使用 `PersonaWindow.Open(int npcId, int taiwuId, string npcName, TMP_FontAsset font)`；综合配置使用「系统设置 → AI 设置」。

## Review Focus

- 世界退出时旧异步任务仍可能回调：只能删除旧窗口自己的清理，不得影响其他窗口的代际失效；由 Task 1 和 Task 4 覆盖。
- 旧源码断言可能仍保护现行存储行为：必须迁移到当前实现，不能只删除断言；由 Task 3 覆盖。
- 人设入口误回旧窗口：当前 NPC 必须继续进入 `PersonaWindow`；由 Task 4 的实机检查覆盖。
- 删除后程序集仍残留类型引用：必须通过完整 Release 构建，而非仅搜索调用点；由 Task 2 覆盖。
- 玩家数据误删：计划不包含任何配置或存档文件删除；由 Task 4 检查部署差异覆盖。

---

### Task 1: 断开生产生命周期依赖

**Files:**
- Modify: `src/JianghuYouling.Frontend/Game/WorldLifecycle.cs:30-49`
- Modify: `src/JianghuYouling.Frontend/UI/ConfigHost.cs`
- Modify: `src/JianghuYouling.Frontend/Plugin.cs`
- Test: `tools/regression-checks.ps1`

**Interfaces:**
- Consumes: `WorldLifecycle.ResetWorldScopedUi()` 的现有逐窗口清理链。
- Produces: 不再引用 `ConfigWindow` 的生产程序集；其他窗口的 `ResetForWorldExit()` 调用保持原顺序。

- [ ] **Step 1: 写入失败回归**

在 `tools/regression-checks.ps1` 增加检查：生产 `src` 不得调用 `ConfigWindow.ResetForWorldExit()`；`HistoryWindow`、`ChatHistoryWindow`、`NpcNovelWindow`、`PersonaWindow`、`WorldBookWindow`、`StructuredWorldBookWindow`、`AISettingsWorldBookEditor` 和 `AISettingsMonthlyCandidateView` 的世界退出调用仍保留。

- [ ] **Step 2: 验证回归按预期失败**

Run: 使用项目现有 Windows PowerShell UTF-8 包装命令运行 `tools/regression-checks.ps1`。
Expected: 新增的 ConfigWindow 生命周期禁止检查失败；其他既有结果单独列出。

- [ ] **Step 3: 删除唯一生产调用并更新注释**

从 `WorldLifecycle.ResetWorldScopedUi()` 删除 `ConfigWindow.ResetForWorldExit()`，不改变相邻窗口调用。更新 `ConfigHost` 与 `Plugin` 中仍把旧窗口描述为当前协程或存盘入口的注释；不得改变它们的运行逻辑。

- [ ] **Step 4: 验证新增回归通过**

Run: 同 Step 2。
Expected: 新增生产依赖检查通过，其他窗口清理检查仍通过。

### Task 2: 删除旧窗口实现

**Files:**
- Delete: `src/JianghuYouling.Frontend/UI/ConfigWindow.cs`
- Test: `src/JianghuYouling.Frontend/JianghuYouling.Frontend.csproj`

**Interfaces:**
- Consumes: Task 1 完成后的无 ConfigWindow 生产依赖状态。
- Produces: 不再包含旧 `ConfigWindow` 类型的前端程序集。

- [ ] **Step 1: 删除前再次核对生产引用**

Run: 在 `src` 搜索 `ConfigWindow`。
Expected: 不出现任何生产方法调用或类型引用；文档注释若仍有历史说明，不应被误当成运行依赖。

- [ ] **Step 2: 删除旧文件**

仅删除 `src/JianghuYouling.Frontend/UI/ConfigWindow.cs`。项目使用 SDK 默认通配编译，不添加新的排除项或替代窗口。

- [ ] **Step 3: 验证前端完整构建**

Run: `dotnet build src/JianghuYouling.Frontend/JianghuYouling.Frontend.csproj -c Release -warnaserror`
Expected: 0 警告、0 错误；若出现缺失类型或方法，恢复为可读错误清单并回到 Task 1 补齐真实依赖，不建立兼容空壳。

### Task 3: 迁移旧源码回归到现行实现

**Files:**
- Modify: `tools/regression-checks.ps1`
- Modify: `tools/JianghuYouling.DevTest/Program.cs`
- Modify: `tools/JianghuYouling.DevTest/AISettingsAssistantTests.cs`

**Interfaces:**
- Consumes: 已删除的旧文件与当前 `AISettings*`、`PersonaWindow`、Store 实现。
- Produces: 不再读取旧 `ConfigWindow.cs`，且现行行为保留有效断言的测试套件。

- [ ] **Step 1: 建立旧断言处置清单**

按 `configWindow`、`_configWindow2` 和 `ConfigWindow` 搜索三份测试文件。逐项标记为「仅验证已删除 UI」「已由系统设置覆盖」「仍需迁移到 Store / 当前窗口」。157 处 `configWindow` 变量出现不代表 157 个独立行为，必须按断言逐条核对。

- [ ] **Step 2: 先让缺失旧文件检查失败**

Run: 使用现有 PowerShell 包装命令运行结构回归。
Expected: 任何仍读取 `ConfigWindow.cs` 的检查明确报缺失文件；不得提前删除失败输出。

- [ ] **Step 3: 删除纯旧 UI 断言并迁移有效行为断言**

删除仅验证旧窗口尺寸、旧导航和旧按钮布局的检查。模型、媒体、对话、过月、助手和存储行为仍有效时，把断言目标改为对应的 `AISettings*Pane`、当前 Store 或 `PersonaWindow`；不得保留只验证旧实现字面量的检查。

- [ ] **Step 4: 运行结构回归与 DevTest**

Run: 先运行 `tools/regression-checks.ps1`，再运行 `dotnet run --project tools/JianghuYouling.DevTest/JianghuYouling.DevTest.csproj -c Release`。
Expected: 不再读取或引用旧 `ConfigWindow.cs`；新增与迁移检查通过。既有反编译基线缺失、既有手动总结失败须原样报告，不纳入本次修复。

### Task 4: 部署与实机验收

**Files:**
- Modify: `docs/acceptance-checklist.md`
- Verify: 本地运行目录中的 `Plugins/JianghuYouling.Frontend.dll`

**Interfaces:**
- Consumes: Task 1–3 的完整删除与测试结果。
- Produces: 可复核的本地部署和手动验收记录。

- [ ] **Step 1: 完成最终构建并核对部署**

Run: `dotnet build src/JianghuYouling.Frontend/JianghuYouling.Frontend.csproj -c Release -warnaserror`，再分别计算构建产物与本地运行目录前端 DLL 的 SHA-256。
Expected: 构建 0 警告、0 错误，两个哈希一致。

- [ ] **Step 2: 检查部署不包含数据删除**

核对本次文件差异只包含旧代码、测试和验收文档；不得出现 `Settings`、人设、世界书、模型档案或存档文件删除。

- [ ] **Step 3: 更新手动验收清单**

在 `docs/acceptance-checklist.md` 记录待验证项：聊天窗「人设」打开当前 NPC 编辑器；系统设置九个子标签可用；进入、退出和切换存档无旧窗口残留或异常；原生还原、模型档案、语音和生图开关正常。

- [ ] **Step 4: 由用户执行实机验收**

Expected: 上述场景均无缺失入口、旧窗口重开、异常日志或配置丢失。未获得实机结果前，不把删除标记为全部验收完成。
