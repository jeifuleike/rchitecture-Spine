---
title: 'Story 1.2: 用本地 SQLite 保存最小专注片段'
type: 'feature'
created: '2026-08-26'
status: 'draft'
review_loop_iteration: 0
story_key: '1-2-用本地-sqlite-保存最小专注片段'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** 当前应用只有内存中的后台宿主；退出或异常后没有可恢复的活动事实，后续采样与日报表也没有可靠的数据基础。

**Approach:** 建立 Domain、Application、Infrastructure 三层的最小记录存储切面，以当前用户本地 SQLite 作为唯一事实源。用合成采样驱动的协调器创建和延展最小片段，并在启动时安全收拢遗留开放片段。

## Boundaries & Constraints

**Always:** 数据库位于 `%LocalAppData%/FocusRecorder`；schema 迁移串行且在事务中执行；SQLite 主键为 UUID 文本，时间使用 UTC ISO 8601，片段同时存 `local_date` 与 UTC 偏移；只有 `RecordingCoordinator` 能创建、延展、关闭或 checkpoint 片段；开放片段创建即落盘，每 30 秒 checkpoint；`focus` 必有稳定应用身份，`idle`、`paused`、`locked`、`sleeping`、`excluded` 必无身份；错误和日志不得含路径、窗口标题或内容级活动信息。

**Ask First:** 如需更改目标框架、存储到用户目录外、引入网络、账号、遥测、真实 Win32 调用，或将分类、排除、报表、导出提前纳入本故事，必须停止并询问。

**Never:** 不采集或持久化窗口标题、完整文件路径、网页/文档内容、聊天对象、键盘输入、鼠标轨迹、截图或进程 ID；不创建分类、分类规则、排除规则或导出 schema；不在 UI 中直接写 SQLite。

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| 首次初始化 | 本地数据库不存在 | 创建目录、迁移表、片段/身份/设置/日志 schema | 初始化失败转为无敏感信息的本地错误 |
| focus 信号 | 合成身份与 UTC 时间有效 | 创建或延展带身份、日期和偏移的开放 `focus` 片段 | 事务失败不留下部分写入 |
| 非专注状态 | idle/paused/locked/sleeping/excluded 信号 | 写入不带应用身份的片段，且不作为 focus | 违反身份约束时拒绝写入 |
| 启动恢复 | 存在遗留开放片段 | 关闭到最后 checkpoint，随后等待新合格信号 | 无 checkpoint 时不虚构活动时长 |

</frozen-after-approval>

## Code Map

- `src/FocusRecorder.App/App.xaml.cs` -- 当前组合根；负责初始化基础设施、协调器并在退出时有序停止。
- `src/FocusRecorder.App/Services/BackgroundHostService.cs` -- 现有幂等宿主；保持 WPF 生命周期接缝，不承载持久化规则。
- `src/FocusRecorder.App/FocusRecorder.App.csproj` -- 现有 `net10.0-windows` WPF 项目；需引用分层项目而非直接堆积数据访问代码。
- `_bmad-output/implementation-artifacts/epic-1-context.md` -- Epic 1 的 SQLite、隐私、状态、checkpoint 与命名不变量。
- `_bmad-output/planning-artifacts/epics.md` -- Story 1.2 验收条件及仅限最小 schema 的范围边界。

## Tasks & Acceptance

**Execution:**
- [ ] `FocusRecorder.sln`、`src/FocusRecorder.Domain/`、`src/FocusRecorder.Application/`、`src/FocusRecorder.Infrastructure/` -- 建立可被 WPF 壳引用的分层项目和项目引用，保持单进程部署。
- [ ] `src/FocusRecorder.Domain/` -- 定义 `FocusSegment`、`ApplicationIdentity`、稳定片段状态和不泄露敏感字段的验证规则。
- [ ] `src/FocusRecorder.Application/RecordingCoordinator.cs` 与端口 -- 用单序列处理合成 focus/非专注信号、延展、关闭和 30 秒 checkpoint，不依赖真实 Win32。
- [ ] `src/FocusRecorder.Infrastructure/Sqlite/` -- 提供数据库路径、事务迁移、片段/身份/设置/结构化日志仓储及遗留开放片段恢复；仅创建本故事允许的表。
- [ ] `src/FocusRecorder.App/App.xaml.cs` -- 组合数据库初始化和协调器，启动恢复、退出时关闭开放片段，不改变现有托盘关闭/隐藏语义。
- [ ] `tests/FocusRecorder.Domain.Tests/`、`tests/FocusRecorder.Application.Tests/`、`tests/FocusRecorder.Infrastructure.Tests/` -- 使用临时 SQLite 与合成信号验证 schema、身份约束、checkpoint、恢复和原子失败；不调用真实 Win32。
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- 在实现和验证完成后将 Story 1.2 推进到 `review`。

**Acceptance Criteria:**
- Given 首次运行且不存在数据库, when 数据层初始化, then `%LocalAppData%/FocusRecorder` 中创建 SQLite 文件和最小 schema，且不包含分类、排除或导出表。
- Given 协调器接收合格 focus 采样, when 写入或延展片段, then 保存 UTC 边界、本地日期、UTC 偏移和稳定应用身份。
- Given 协调器写入非专注状态, when 仓储持久化, then 应用身份为空，且该片段不会作为 focus 统计来源。
- Given 进程异常后重新启动, when 发现开放片段, then 将其闭合到最后 checkpoint，且直到新合格信号才创建新片段。

## Spec Change Log

## Design Notes

本故事建立“协调器独占写入”而不是把生命周期状态直接映射为 SQL。这样 Story 1.3 可以只提供安全的应用级信号，Story 1.4 可以只增加状态边界，而不会绕过片段一致性和 checkpoint 语义。

## Verification

**Commands:**
- `dotnet test FocusRecorder.sln` -- expected: 全部领域、应用和 SQLite 测试通过，且测试数据均为合成数据。
- `dotnet build FocusRecorder.sln` -- expected: WPF 壳和所有分层项目在 .NET 10 SDK 环境中成功编译。

**Manual checks (if no CLI):**
- 检查 schema 仅包含 migrations、focus segments、application identities、settings、structured logs；检查持久化模型与日志字段没有标题、路径、内容或进程 ID。
