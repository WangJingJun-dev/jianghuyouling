# 开发与测试

## 本机配置

安装 .NET SDK 和合法的《太吾绘卷》，然后从仓库根目录运行：

```powershell
pwsh -File tools/bootstrap-development.ps1 -GameRoot "你的太吾绘卷安装目录" -SkipDecompile
```

脚本生成被 Git 忽略的 `Directory.Build.local.props`，并还原 NuGet 依赖。
也可设置 `TAIWU_GAME_DIR`。
不提交本机配置、游戏 DLL、运行日志、存档或密钥。

## 设置界面与诊断

模型、体验档位、语音、生图、对话、世界书、过月、助手配置位于游戏「系统设置 → AI设置」，通过原生设置项保存并热重载。旧 ConfigWindow 暂时只保留 NPC 人设对比入口，不再向这些配置写入数据。世界书在主界面置灰，进入存档后才允许编辑。

`[JHYL_AI_SETTINGS]` 诊断保持开启，覆盖分类同步、控件初始化、保存和热重载结果；日志不得包含密钥、完整人设、世界书或其他敏感正文。与独立「太吾界面优化」同时修改系统设置分类时，遵守工作区根目录 `AGENTS.md` 的幂等与恢复契约。

## 构建与离线测试

```powershell
dotnet build src/JianghuYouling.Frontend/JianghuYouling.Frontend.csproj -c Debug -warnaserror
dotnet build src/JianghuYouling.Backend/JianghuYouling.Backend.csproj -c Debug -warnaserror
dotnet run --project tools/JianghuYouling.DevTest/JianghuYouling.DevTest.csproj -c Debug -- --offline
dotnet run --project tools/JianghuYouling.SecretStorageTests/JianghuYouling.SecretStorageTests.csproj -c Debug
dotnet run --project tools/JianghuYouling.TrustBoundaryTests/JianghuYouling.TrustBoundaryTests.csproj -c Debug
dotnet run --project tools/JianghuYouling.BackendContractTests/JianghuYouling.BackendContractTests.csproj -c Debug
pwsh -File tools/test-workshop-package-validator.ps1
pwsh -File tools/test-real-game-e2e-evidence-gate.ps1
```

Debug 不写入游戏目录。Release 默认只自动部署到本机游戏 `Mod` 目录。Steam 工坊下载目录只用于拉取线上发布版，不用于测试部署或构建输出。
只验证编译时使用 Debug，或通过 MSBuild 的 `ModDeploy` 属性显式重定向测试部署目录。

BackendContractTests 还需要下面说明的本地游戏接口快照；生成后设置
`JHYL_DECOMPILED_ROOT`，或使用默认的 `.decompiled` 目录，再运行此项测试。

`tools/full-mod-e2e.ps1` 是自动化预检，不是真实游戏端到端测试。
它会构建并核对部署文件，不应在不希望更新本机 Mod 时直接运行。
在线模型测试需主动启用并提供本机受保护配置，切勿将配置上传。

## 本地游戏接口校验

`tools/decompile-latest.ps1` 可为自己的游戏安装生成本地接口核对资料。
设置 `JHYL_DECOMPILED_ROOT` 指定输出位置；否则使用仓库内被忽略的 `.decompiled`。
不将生成的游戏源码发布或随 Mod 分发。

当前接口回归针对 Steam build **25596993**，原生程序集 **35** 个，
反编译 C# 清单 **11,585** 项。生成完整快照后，可运行：

```powershell
pwsh -File tools/regression-checks.ps1
```

游戏升级可能改变接口和源码清单；检查本机 `VERSION.txt` 与脚本中的契约要求。

## 发布包

Release 构建完成后，`tools/prepare-test-workshop.ps1 -ReleaseVersion 0.34.0.41`
创建仅含声明文件的测试工坊候选包；创建候选包不会上传。
包中包括本项目三个 DLL、Mod 清单、展示资源及许可文件。
不要加入游戏程序集、用户配置、存档或日志。

使用包哈希清单及 `tools/validate-workshop-package.ps1` 验证候选内容。
真实游戏回归还需遵循 [数据安全](real-game-e2e-data-safety.md)
和 [验收清单](acceptance-checklist.md)，不能以离线测试代替。
