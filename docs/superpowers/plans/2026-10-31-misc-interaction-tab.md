# 原生「杂务」互动标签实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在原生 NPC 互动窗增加可用的「杂务」标签，同时保留现有对话窗口按钮。

**Architecture:** 克隆原生标签并注入 `CToggleGroup`，仅对杂务索引绕过原生选择回调。在 `mainWindowRoot` 内构建操作区，通过活动对话页签复用已有操作。

**Tech Stack:** C#、Unity UI、Harmony、PowerShell 回归脚本、.NET。

**Spec:** `docs/superpowers/specs/2026-10-31-misc-interaction-tab-design.md`

## Global Constraints

- 不修改原对话窗口按钮。
- 不改变其他原生标签的行为和 `EventCommonOptionSelect` 参数。
- 不复制六个操作的业务实现。
- 不部署到工坊目录。

## Task 1: 建立失败测试

- [ ] 在 `tools/regression-checks.ps1` 增加以下检查并确认旧代码失败：杂务标签名、`CToggleGroup.Add`、Harmony 选中拦截、六个按钮文本、活动页签转发、原按钮保留。

## Task 2: 实现杂务标签与操作区

- [ ] 新建 `MiscInteractionTab.cs`，负责注入、显隐和按钮创建。
- [ ] 扩展 `EwReflect`，读取原生 `interactionTypeTogGroup`。
- [ ] 在 `ChatWindow` 暴露单一杂务转发方法，复用活动页签的六个操作及静态窗口入口。
- [ ] 运行红灯对应的全部新检查，确认只剩基线快照缺失。

## Task 3: 验证、部署和路线图

- [ ] 构建前后端 Debug `-warnaserror`，运行 TrustBoundary、SecretStorage、DevTest。
- [ ] 部署到本地 `Mod\JianghuYouling`，核对加载来源。
- [ ] 独立审查标签切换、转发和生命周期。
- [ ] 生成主动来信渐进路线图 HTML，保留频率与熟悉度权重，逐层加入动机、关系、七元赋性和距离。
- [ ] 记录实机验收项，未实际验收不得宣称通过。
