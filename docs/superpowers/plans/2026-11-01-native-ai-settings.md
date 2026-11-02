# Native AI Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把太吾有灵的模型、体验档位、语音、生图、对话、世界书、过月和助手设置完整迁入游戏原生「AI设置」，并保留即时保存与热重载。

**Architecture:** `AISettingsHost` 只负责分类、Holder、Toggle 和刷新协调；原生自定义控件、各子项设置描述、复杂内嵌编辑器分别放在小文件中。所有控件继续调用现有 Store、事务和运行层通知，不建立第二套配置系统。分类同步每次 Refresh 执行，并与独立「太吾界面优化」MOD 遵守相同的幂等恢复契约。

**Tech Stack:** C#、.NET Framework 4.8、Unity uGUI、TextMeshPro、Harmony、现有 DevTest 与 PowerShell 回归脚本。

**Spec:** `docs/superpowers/specs/2026-11-01-native-ai-settings-design.md`

## Global Constraints

- 开发目录固定为 `E:\Work\VibeCoding\TaiWuDev\JianghuYouling`，本地部署目录固定为 `D:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\JianghuYouling`。
- 八个子项全部原生内嵌；不得打开旧 ConfigWindow，不新增 Agent 风格窗口。
- NPC「人设」本轮不迁移、不删除，等待用户实机对比。
- 保留 DPAPI、原子写入、事务回滚、世界隔离、长文本限制和现有功能完整性。
- 诊断模式保持开启：使用 `[JHYL_AI_SETTINGS]` 输出分类同步、控件初始化、保存和热重载结果；不得输出密钥、完整人设、世界书或其他敏感正文。
- 不上传工坊，不提交 Git，不修改无关既有失败测试。
- 中文与英文、数字之间按项目文档规范留白；界面分类名固定为「AI设置」。

## Review Focus

- 语言切换把分类字段替换为 `String[]`：两个 MOD 应在同一次 Refresh 中恢复并保留彼此修改；由 Task 1 的静态契约和双 MOD 实机检查覆盖。
- 默认助手人设仅用于显示：打开页面或保存其他助手项不得把默认文本写成自定义；由 Task 3 的存储测试覆盖。
- 中文 IME 与手柄焦点：输入可正常使用，周围原生项不被新控件截断；由 Task 2 的导航契约和实机检查覆盖。
- 世界切换时仍有旧读取或保存任务：不得把旧世界内容写入新世界；由 Task 8 的代际与 WorldId 测试覆盖。
- 密钥或关联配置写入失败：不得明文记录、部分生效或伪报成功；由 Task 5、Task 6 的事务与脱敏检查覆盖。

---

### Task 1: 可组合的分类同步与诊断

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/AISettingsCategorySync.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsHost.cs`
- Modify: `tools/regression-checks.ps1`

**Interfaces:**
- Produces: `AISettingsCategorySync.Synchronize(SettingCategoryData[] current, SettingCategoryData[] nativeBaseline)`，返回保留现有修改且只追加一个 AI 分类的新数组。
- Produces: `AISettingsCategorySync.RequiresRecovery(System.Array current)`，仅在当前数组不是 `SettingCategoryData[]` 时返回 `true`。
- Consumes: 原生五分类快照仅用于 `RequiresRecovery` 为 `true` 的恢复路径。

- [ ] **Step 1: 写入失败回归**

在 `tools/regression-checks.ps1` 增加 `AISettingsCategorySync` 检查，断言：有效数组从 `current` 同步、AI 分类去重、仅在类型不兼容时使用 `nativeBaseline`、每次 Refresh 都调用同步、诊断标签为 `[JHYL_AI_SETTINGS]`，并禁止恢复路径使用 `_baseCategories` 覆盖有效当前数组。

- [ ] **Step 2: 验证回归按预期失败**

Run: 使用项目现有 Windows PowerShell 包装命令运行 `tools/regression-checks.ps1`。
Expected: 新增检查失败；既有反编译基线缺失仍单独列出。

- [ ] **Step 3: 实现最小分类同步**

创建 `AISettingsCategorySync`，将独立 AI 分类和八个子项定义集中在该方法使用的目录中。`AISettingsHost.Initialize` 保存原生基线，`EnsureCategories` 在有效数组上调用 `Synchronize(current, null)`，仅在类型失效时调用 `Synchronize(null, nativeBaseline)`。保留独立标题「AI设置」，不修改 Game 分类。

- [ ] **Step 4: 接入始终开启的诊断**

记录输入类型、是否走恢复路径、最终 `Category` 顺序和 Holder/Toggle 数量；日志不得记录设置值。禁止添加关闭诊断的默认开关。

- [ ] **Step 5: 运行构建与新增回归**

Run: `dotnet build src/JianghuYouling.Frontend/JianghuYouling.Frontend.csproj -c Release -warnaserror`
Expected: 0 警告、0 错误，新增回归通过；已知基线缺失不作为本任务新增失败。

### Task 2: 原生单行、多行与动作控件

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/NativeTextSettingAttribute.cs`
- Create: `src/JianghuYouling.Frontend/UI/NativeTextSettingItem.cs`
- Create: `src/JianghuYouling.Frontend/UI/NativeActionSettingAttribute.cs`
- Create: `src/JianghuYouling.Frontend/UI/NativeActionSettingItem.cs`
- Create: `src/JianghuYouling.Frontend/UI/NativeSettingControlFactory.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsHost.cs`
- Modify: `tools/regression-checks.ps1`

**Interfaces:**
- Produces: `NativeTextSettingAttribute(ESettingSubCategory, int, LanguageKey)`，属性 `Multiline`、`Password`、`CharacterLimit`，`UIType` 使用专用扩展值。
- Produces: `NativeActionSettingAttribute(ESettingSubCategory, int, LanguageKey)`，属性 `ActionText`，`UIType` 使用另一专用扩展值。
- Produces: `NativeSettingControlFactory.Register(Dictionary<SettingUIType, SettingItemBase> prefabMap, ViewSystemSetting window)`，注册可克隆的原生风格控件。
- Consumes: Task 1 的八个子项和分类 Holder。

- [ ] **Step 1: 写入失败控件契约**

断言文本控件实现 `Initialize`、`GetValue`、`SetValue`、`SetInteractable` 和 `SetTypedValue(string)`；单行在结束编辑时提交，多行只在显式保存时提交；密码使用掩码；动作按钮不通过两个相同 SwitchButton 选项模拟。

- [ ] **Step 2: 验证契约失败**

Run: 项目 Windows PowerShell 回归命令。
Expected: 新控件文件或方法不存在导致新增检查失败。

- [ ] **Step 3: 实现并注册控件**

从现有设置项克隆标签、背景和布局，右侧创建 TMP 输入或动作按钮。多行区包含视口、滚动条和原生页内保存按钮；动作项提供单一明确按钮。初始化失败记录类型和子项，不记录输入正文。

- [ ] **Step 4: 接入导航与 IME 边界**

保留 TMP 中文 IME 输入；为每个 `Selectable` 配置显式上下左右导航，不让多行滚动区吞掉周围分类切换。文本编辑以键鼠完成，不新增手柄虚拟键盘。

- [ ] **Step 5: 构建与实机控件烟测**

Run: Task 1 构建命令。
Expected: 构建通过；测试子项中的短文本、多行、掩码、动作按钮均可显示和操作，诊断无初始化异常。

### Task 3: 助手完整迁移与默认人设显示

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/AISettingsAssistantPane.cs`
- Modify: `src/JianghuYouling.Frontend/Game/AssistantPersonaStore.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsHost.cs`
- Modify: `tools/JianghuYouling.DevTest/JianghuYouling.DevTest.csproj`
- Create: `tools/JianghuYouling.DevTest/AISettingsAssistantTests.cs`
- Modify: `tools/JianghuYouling.DevTest/Program.cs`

**Interfaces:**
- Produces: `AssistantPersonaStore.LoadDisplay()`，自定义非空时返回自定义，否则返回 `SpecialPersonaCatalog.AssistantPersona`。
- Produces: `AssistantPersonaStore.SaveDisplay(string text)`，空白或与内置默认完全一致时清空自定义，否则保存自定义。
- Produces: `AISettingsAssistantPane.CreateInfos()`，返回总开关、两个频率和三个独立助手参数的原生设置项。

- [ ] **Step 1: 写入默认人设失败测试**

测试空自定义时 `LoadDisplay()` 等于完整内置默认；默认文本经 `SaveDisplay` 后 `Load()` 仍为空；修改后保存为自定义；清空后再次显示默认。另断言三个参数名称分别为「助手名字」「助手形象图」「助手人设」。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet run --project tools/JianghuYouling.DevTest/JianghuYouling.DevTest.csproj -c Debug -- --ai-settings-only`
Expected: 新测试因 `LoadDisplay`、`SaveDisplay` 或三个独立参数不存在而失败。

- [ ] **Step 3: 实现存储与助手设置项**

复用 `AssistantNameStore`、`AssistantFacePathStore`、`AssistantEnabledStore`、频率 Store 和 Task 2 控件。名字和形象路径结束编辑后保存，形象保存后调用两个现有助手刷新通知；多行人设显式保存。保存失败恢复磁盘值并输出脱敏诊断。

- [ ] **Step 4: 验证默认显示和热重载**

Run: Step 2 命令及 Frontend Release 构建。
Expected: 新测试通过；实机显示完整默认性格，保存其他助手项不会生成自定义人设文件内容，三项修改无需重启即可生效。

### Task 4: 对话与过月的简单设置迁移

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/AISettingsConversationPane.cs`
- Create: `src/JianghuYouling.Frontend/UI/AISettingsMonthlyPane.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsHost.cs`
- Modify: `tools/regression-checks.ps1`

**Interfaces:**
- Produces: `AISettingsConversationPane.CreateInfos()` 和 `AISettingsMonthlyPane.CreateInfos()`。
- Consumes: Task 2 原生控件；现有对话、字体、主动消息和过月 Store。

- [ ] **Step 1: 写入设置清单与刷新失败回归**

逐项断言设计中的对话和过月设置都有独立条目；保存后分别调用 `TalkOrchestrator` 状态同步、字体绑定、`NpcProactiveChatScheduler.NotifySettingsChanged()` 或 `MonthlyDigestPopup.NotifySettingsChanged()`。主动人物数量仍限制为 0～8。

- [ ] **Step 2: 验证回归失败**

Run: 项目 Windows PowerShell 回归命令。
Expected: 两个 Pane 尚未实现导致新增检查失败。

- [ ] **Step 3: 实现即时保存和运行刷新**

将现有循环按钮逻辑映射为原生选项值，将数量输入映射为带校验的文本项。思考表达提示词使用 Task 2 多行控件；不得调用 `ThinkingPromptEditorWindow` 或旧 ConfigWindow。

- [ ] **Step 4: 构建并实机核对**

Run: Frontend Release 构建。
Expected: 所有当前值正确显示；修改后不重启即生效，保存失败有诊断且界面回读真实值。

### Task 5: 模型、档案与接口测试原生内嵌

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/AISettingsModelPane.cs`
- Create: `src/JianghuYouling.Frontend/UI/AISettingsModelProfileView.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsHost.cs`
- Modify: `tools/JianghuYouling.DevTest/AISettingsAssistantTests.cs`（更名为通用 AI 设置测试时同步更新引用）
- Modify: `tools/regression-checks.ps1`

**Interfaces:**
- Produces: `AISettingsModelPane.CreateInfos()` 和 `AISettingsModelProfileView` 的页内档案列表、选择、保存、激活、删除接口。
- Consumes: `LlmService`、`LlmProfileStore`、`SettingsBatchTransaction`、Task 2 控件。

- [ ] **Step 1: 写入模型字段与失败恢复测试**

覆盖接口地址、密钥、主模型、后台模型、回复上限、上下文窗口的当前值读取；断言密钥掩码与日志脱敏、档案操作事务回滚、成功后 `LlmService.Reload()`，并禁止调用 `ConfigWindow.Open`。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet run --project tools/JianghuYouling.DevTest/JianghuYouling.DevTest.csproj -c Debug -- --ai-settings-only`
Expected: 模型 Pane 和页内档案视图不存在导致失败。

- [ ] **Step 3: 实现完整模型设置与档案视图**

在子项内展开档案列表和操作，保留滚动、长名称、保存当前为档案、立即切换和删除。接口测试复用现有取消与超时逻辑，在原生状态行显示结果。

- [ ] **Step 4: 验证失败路径和热重载**

Run: Step 2 命令及 Release 构建。
Expected: 测试通过；模拟写入失败不部分生效，日志不含密钥，成功保存后下一轮模型调用使用新配置。

### Task 6: 语音与生图完整迁移

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/AISettingsVoicePane.cs`
- Create: `src/JianghuYouling.Frontend/UI/AISettingsImagePane.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsHost.cs`
- Modify: `tools/regression-checks.ps1`

**Interfaces:**
- Produces: `AISettingsVoicePane.CreateInfos()` 和 `AISettingsImagePane.CreateInfos()`。
- Consumes: `TtsConfig`、`TtsSettings`、`ImageGenerationConfig` 和 Task 2 控件。

- [ ] **Step 1: 写入完整字段和脱敏失败回归**

断言语音三个连接字段与朗读内容、生图全部字段和水印均存在；输出尺寸只读并随协议更新；两个密钥均掩码且独立保存；不得复用主模型密钥。

- [ ] **Step 2: 验证回归失败**

Run: 项目 Windows PowerShell 回归命令。
Expected: 两个 Pane 尚未实现导致新增检查失败。

- [ ] **Step 3: 实现保存与现有校验**

保留旧默认迁移、ComfyUI 工作流校验、本地地址限制和现有默认值。保存失败恢复真实磁盘值；诊断只记录字段名、结果和脱敏 endpoint。

- [ ] **Step 4: 构建并验证现有运行路径**

Run: Frontend Release 构建、DevTest 中 TTS 与生图相关测试。
Expected: 新设置页与既有朗读、生图路径使用同一配置，相关测试无新增失败。

### Task 7: 体验档位原子切换

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/AISettingsExperiencePane.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsHost.cs`
- Modify: `tools/regression-checks.ps1`

**Interfaces:**
- Produces: `AISettingsExperiencePane.CreateInfos()`，提供三个档位、当前配置摘要和应用动作。
- Consumes: `ExperiencePresetPolicy` 与 `SettingsBatchTransaction`。

- [ ] **Step 1: 写入档位边界失败回归**

断言三个档位只修改过月、助手主动频率、群聊轮数和默认同道数量，不修改模型、Key、上下文、回复上限、人设或世界书；应用使用一个事务并刷新受影响的原生设置项。

- [ ] **Step 2: 验证回归失败**

Run: 项目 Windows PowerShell 回归命令。
Expected: 体验 Pane 不存在导致失败。

- [ ] **Step 3: 实现页内档位和原子应用**

使用原生选项和摘要文本展示三个档位，应用后重新读取所有相关 Store，失败按现有事务恢复并提示。

- [ ] **Step 4: 构建与联动检查**

Run: Frontend Release 构建。
Expected: 切换后助手、对话和过月子项显示一致的新值，运行层同步刷新。

### Task 8: 世界书完整结构化内嵌与存档门禁

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/AISettingsWorldBookPane.cs`
- Create: `src/JianghuYouling.Frontend/UI/AISettingsWorldBookEditor.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsHost.cs`
- Modify: `tools/JianghuYouling.DevTest/JianghuYouling.DevTest.csproj`
- Create: `tools/JianghuYouling.DevTest/AISettingsWorldBookTests.cs`
- Modify: `tools/JianghuYouling.DevTest/Program.cs`

**Interfaces:**
- Produces: `AISettingsWorldBookPane.CreateInfos()` 和页内结构化编辑器；不得调用 `StructuredWorldBookWindow.Open`。
- Consumes: `StructuredWorldBookStore`、`WorldBookStore`、`LongTextEditorOperationGate`、`WorldLifecycle` 和 Task 2 控件。

- [ ] **Step 1: 写入世界状态失败测试**

覆盖无世界身份时禁用、有效存档时加载、分类/条目/开关/常驻或关键词/同层优先级、导入导出、增删保存与恢复默认。旧世界 Generation 或 WorldId 的完成回调必须被丢弃。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet run --project tools/JianghuYouling.DevTest/JianghuYouling.DevTest.csproj -c Debug -- --ai-settings-only`
Expected: 世界书页内编辑器不存在导致失败。

- [ ] **Step 3: 提取并嵌入完整编辑器**

把 `StructuredWorldBookWindow` 的文档操作和视图投影提取为可复用组件，原生 Holder 负责呈现。保留后台读取、事务、长文本限制、导入导出和草稿保护，不保留旧窗口打开路径。

- [ ] **Step 4: 验证主界面、切档和回档**

Run: Step 2 命令及 Release 构建。
Expected: 测试通过；主界面全部编辑控件置灰并提示进入存档，切档后不串内容，保存后下一轮对话读取新快照。

### Task 9: 主动人物名单页内管理

**Files:**
- Create: `src/JianghuYouling.Frontend/UI/AISettingsMonthlyCandidateView.cs`
- Modify: `src/JianghuYouling.Frontend/UI/AISettingsMonthlyPane.cs`
- Modify: `tools/regression-checks.ps1`

**Interfaces:**
- Produces: 页内候选人物列表、默认同道标记、移除和重新加入操作。
- Consumes: `CompanionMonthlyCandidateStore`、现有只读人物查询和世界代际校验。

- [ ] **Step 1: 写入名单功能失败回归**

断言名单读取、空状态、默认同道与手动加入标记、移除和重新加入均在原生页内完成，禁止调用 `MonthlyCandidateWindow.Open`，并保留世界身份与请求 epoch 校验。

- [ ] **Step 2: 验证回归失败**

Run: 项目 Windows PowerShell 回归命令。
Expected: 页内名单视图不存在导致失败。

- [ ] **Step 3: 实现页内名单**

复用 `MonthlyCandidateWindow` 的查询与 Store 逻辑，以原生列表呈现；查询期间禁用相关操作，失败显示原因，旧世界回调不得修改新页面。

- [ ] **Step 4: 构建与实机验证**

Run: Frontend Release 构建。
Expected: 名单功能与旧入口能力一致，不打开旧窗口。

### Task 10: 旧入口退场、文档同步与整体验证

**Files:**
- Modify: `src/JianghuYouling.Frontend/UI/ConfigWindow.cs`
- Modify: `src/JianghuYouling.Frontend/Plugin.cs`
- Modify: `README.md`
- Modify: `docs/development.md`
- Modify: `docs/acceptance-checklist.md`
- Modify: `deploy/Config.lua`
- Modify: `tools/regression-checks.ps1`

**Interfaces:**
- Consumes: Task 1～9 的全部原生设置入口。
- Produces: 八个旧子项不再可访问；聊天旧「设置」暂时只提供 NPC「人设」对比入口。

- [ ] **Step 1: 写入旧入口退场失败回归**

断言八个旧导航和保存分支不可达；聊天旧「设置」仍能访问原 NPC 人设页，且 `PersonaWindow` 与其存储均未改变。本轮不得提前把该入口路由到原生 AI 分类。

- [ ] **Step 2: 验证回归失败**

Run: 项目 Windows PowerShell 回归命令。
Expected: 旧入口仍存在导致新增检查失败。

- [ ] **Step 3: 移除重复入口并同步文档**

删除八个已迁移子项的旧字段、导航、按钮和保存分支；只保留本轮明确暂缓的 NPC 人设实现和临时访问路径。更新设置位置、热重载说明和界面描述，注明八项已迁入原生 AI 设置，NPC 人设入口等待用户对比后再决定。

- [ ] **Step 4: 运行自动化验证**

Run: Frontend/Backend Release `-warnaserror` 构建、DevTest `--offline`、TrustBoundary、SecretStorage、BookRenderTests 和回归脚本。
Expected: 本次相关检查通过；既有 `personaVisible` 陈旧断言和反编译基线缺失若仍存在，单独报告，不伪称全绿。

- [ ] **Step 5: 部署并完成实机验收**

Run: 比较构建与本地运行 DLL 的 SHA-256。
Expected: 哈希一致；主界面、存档内、语言切换及与「太吾界面优化」同时启用时分类正确，八子项功能完整、世界书门禁正确、默认助手人设可见、热重载有效，诊断日志开启且无敏感正文。
