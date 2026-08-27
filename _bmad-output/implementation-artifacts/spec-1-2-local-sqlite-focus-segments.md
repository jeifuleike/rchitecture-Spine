---
title: 'Story 1.2: 用本地 SQLite 保存最小专注片段'
type: 'feature'
created: '2026-08-27'
status: 'done'
review_loop_iteration: 0
followup_review_recommended: true
baseline_revision: '29ef83fff79f08bcd0b14bc1bb376fbd9aca1af9'
baseline_commit: '29ef83fff79f08bcd0b14bc1bb376fbd9aca1af9'
story_key: '1-2-用本地-sqlite-保存最小专注片段'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** 现有后台宿主只在内存中保存运行状态；退出或异常后没有可靠、可恢复的活动事实，后续采样与报表无从解释用户的一天。

**Approach:** 在当前 Windows 用户的本地 SQLite 中建立最小的分层记录存储。由单序列的 `RecordingCoordinator` 接收合成信号并独占片段写入、检查点与恢复，WPF 壳只负责组合和生命周期。

## Boundaries & Constraints

**Always:** 数据库位于 `%LocalAppData%/FocusRecorder`；迁移串行且每次写入使用单一 SQLite 事务；主键为 UUID 文本、时间为 UTC ISO 8601，并保存创建/拆分时的 `local_date` 与 UTC 偏移。仅 `RecordingCoordinator` 能创建、延展、关闭或 checkpoint 片段；开放片段创建即落盘、每 30 秒 checkpoint。`focus` 必有稳定应用身份，`idle`、`paused`、`locked`、`sleeping`、`excluded` 必无身份，且只有 `focus` 可作为专注统计来源。错误和结构化日志不得包含路径、窗口标题、内容级活动或进程 ID。

**Block If:** 需要改变目标框架、将数据写到用户目录外、引入网络/账号/遥测、真实 Win32 调用，或提前实现分类、排除、报表或导出。

**Never:** 不采集或持久化窗口/网页/文档标题、完整路径、聊天对象、键鼠输入、鼠标轨迹、截图或长期进程 ID；不创建分类、分类规则、排除规则或导出 schema；UI 不直接写 SQLite；测试不读取真实 Win32 或真实用户活动。

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| 首次初始化 | 默认数据库不存在 | 创建 `%LocalAppData%/FocusRecorder` 目录与仅含 migrations、segments、application identities、settings、structured logs 的最小 schema | 初始化异常转换为不含敏感数据的被动失败状态，不留下半完成迁移 |
| 合格 focus 信号 | 合成稳定身份与有效 UTC 时刻 | 创建或延展带身份、UTC 边界、本地日期和偏移的开放 `focus` 片段；每 30 秒更新 checkpoint | 事务失败时不留下部分身份或片段写入 |
| 非专注信号 | `idle`、`paused`、`locked`、`sleeping` 或 `excluded` | 写入无应用身份的相应片段，且其状态不作为 focus 统计来源 | 任何带身份的非 focus 片段被仓储拒绝 |
| 启动恢复 | 存在遗留开放片段 | 将其关闭到最后 checkpoint，恢复后等待新的合格信号才新建片段 | 缺少 checkpoint 时不虚构活动时长 |

</intent-contract>

## Code Map

- `FocusRecorder.sln:5-8` -- 目前仅含 WPF 壳和壳测试；在此登记 Domain、Application、Infrastructure 及各层测试项目，保持依赖方向清晰。
- `src/FocusRecorder.App/FocusRecorder.App.csproj:3-9` -- `net10.0-windows` WPF 壳；添加对 Application/Infrastructure 的组合根引用，不把 SQL 放入壳项目。
- `src/FocusRecorder.App/App.xaml.cs:15-40` -- `OnStartup` 是单实例确认后的组合根；在 `_host.Start()` 前完成存储初始化和遗留片段恢复，失败时以现有被动不可用状态呈现。
- `src/FocusRecorder.App/App.xaml.cs:42-71` -- `OnExit` 和托盘 `ExitApplication` 共用 `DisposeShell`；协调器关闭与仓储释放必须可重复调用，并在释放数据库前结束记录。
- `src/FocusRecorder.App/Services/BackgroundHostService.cs:4-65` -- 复用幂等宿主和 `StatusChanged` 被动状态接缝，不将领域持久化状态塞入其 `RecordingStatus`。
- `src/FocusRecorder.App/MainWindow.xaml.cs:30-44`、`src/FocusRecorder.App/TrayController.cs:12-53` -- 现有关闭隐藏与托盘退出语义为只读约束；退出仍通过 `ExitApplication`。
- `tests/FocusRecorder.App.Tests/BackgroundHostServiceTests.cs:6-66` -- 现有 xUnit 直接构造和并发事件断言范式；新测试使用临时 SQLite 与合成时钟/信号。
- `_bmad-output/implementation-artifacts/epic-1-context.md:28-35` -- 固定 SQLite 事实源、稳定状态字符串、身份规则、单序列协调器和 UTC/checkpoint 不变量。
- `_bmad-output/planning-artifacts/epics.md:200-230` -- Story 1.2 的产品验收与仅限五类最小表的范围依据。

## Tasks & Acceptance

**Execution:**
- [x] `FocusRecorder.sln`、`src/FocusRecorder.Domain/`、`src/FocusRecorder.Application/`、`src/FocusRecorder.Infrastructure/` -- 新建分层项目、项目引用和必要 NuGet 依赖；Domain 不依赖基础设施，App 只在组合根连接 Application 与 Infrastructure。
- [x] `src/FocusRecorder.Domain/` -- 定义 `FocusSegment`、`ApplicationIdentity`、稳定状态字符串与不泄露敏感字段的验证；强制 focus 身份必填、非 focus 身份为空和 UTC 边界有效。
- [x] `src/FocusRecorder.Application/RecordingCoordinator.cs` 及端口/命令模型 -- 用单一顺序处理合成 focus 与非专注信号，创建、延展、关闭和每 30 秒 checkpoint；定义启动恢复和有序关闭边界，不依赖真实 Win32。
- [x] `src/FocusRecorder.Infrastructure/Sqlite/` -- 实现 LocalAppData 路径、幂等事务迁移、身份/片段/设置/无敏感结构化日志仓储，以及遗留开放片段按最后 checkpoint 收拢；不得创建范围外表。
- [x] `src/FocusRecorder.App/App.xaml.cs` -- 在单实例确认后组合存储、协调器和现有宿主；启动恢复、退出关闭和数据库释放均幂等，且不改变主窗口隐藏或托盘退出语义。
- [x] `tests/FocusRecorder.Domain.Tests/`、`tests/FocusRecorder.Application.Tests/`、`tests/FocusRecorder.Infrastructure.Tests/` -- 用临时 SQLite、可控时钟和合成信号覆盖四种矩阵情景、身份约束、30 秒 checkpoint、原子失败及恢复；不调用真实 Win32。
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- 实现与全部验证完成后将 Story 1.2 更新为 `review`。

**Acceptance Criteria:**
- Given 首次运行且不存在数据库, when 数据层完成初始化, then `%LocalAppData%/FocusRecorder` 创建 SQLite 文件和最小 schema，且不包含分类、排除或导出表。
- Given 协调器接收合格 focus 采样, when 写入或延展片段, then 保存 UTC 边界、本地日期、UTC 偏移和稳定应用身份。
- Given 协调器写入非专注状态, when 仓储持久化, then 应用身份为空，且该片段不成为 focus 统计来源。
- Given 进程异常后重新启动, when 发现开放片段, then 将其闭合到最后 checkpoint，且直到新合格信号才创建新片段。
- Given 用户关闭主窗口或从托盘退出, when 壳处理既有生命周期, then 前者保持隐藏并继续记录，后者有序关闭开放片段且不留新的开放片段。

## Spec Change Log

## Review Triage Log

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 12 (high 8, medium 4)
- defer: 0
- reject: 10 (low 10)
- addressed_findings:
  - `[high]` `[patch]` 仅在 SQLite 写入成功后更新开放片段；处理失败、乱序与关闭边界，避免未落盘或时间倒退的片段状态。
  - `[high]` `[patch]` 为开放片段增加独立的串行 30 秒 checkpoint 调度，并在关闭时取消和等待。
  - `[high]` `[patch]` 让退出失败可重试、避免 UI 同步上下文死锁，并维持初始化失败后的被动不可用壳。
  - `[high]` `[patch]` 收紧应用身份的路径/控制字符隐私校验，并升级 SQLite 原生依赖以消除已知高危漏洞。
  - `[medium]` `[patch]` 启用 SQLite 外键、busy timeout、单一开放片段约束和确定性读取；校验 UTC、受影响行与 filename-only 数据库路径。
  - `[medium]` `[patch]` 新增 SQLite 集成测试，覆盖协调器启动恢复及保存、checkpoint、关闭后的真实持久化边界。

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 10 (high 5, medium 5)
- defer: 0
- reject: 11 (low 11)
- addressed_findings:
  - `[high]` `[patch]` 协调器在关闭开始后拒绝新信号，并在关闭写入失败时恢复可重试状态，避免退出后创建开放片段。
  - `[high]` `[patch]` 同一 focus 身份跨本地日期或 UTC 偏移边界时关闭并新建片段，保留报告所需的日期与偏移事实。
  - `[high]` `[patch]` 托盘退出在关闭失败时恢复退出闩锁；WPF 退出路径无论关闭结果都会释放壳资源。
  - `[medium]` `[patch]` 拒绝非正 checkpoint 间隔，防止紧密循环或后台计时器故障。
  - `[medium]` `[patch]` checkpoint 事务失败不会终止后续周期调度；下一个周期可继续持久化。
  - `[medium]` `[patch]` 生命周期退出用原子闩锁串行化，同时保留失败后的再次退出能力。
  - `[medium]` `[patch]` 应用身份拒绝可执行文件名中的控制字符，并验证片段 UUID、本地日期和 UTC 偏移的一致性。
  - `[medium]` `[patch]` 初始化改为幂等，失败时允许安全重试而不遗留计时器状态。

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 8 (high 5, medium 3)
- defer: 0
- reject: 12 (low 12)
- addressed_findings:
  - `[high]` `[patch]` 状态切换关闭旧片段后立即清除内存开放引用，替换片段落盘失败时后续信号可重新创建，不会反复关闭旧行。
  - `[high]` `[patch]` 关闭失败且仍有开放片段时恢复 checkpoint 调度，保留重试期间的 30 秒持久化保证。
  - `[high]` `[patch]` SQLite 关闭拒绝早于最后 checkpoint 的结束时间，避免存储不满足片段时间不变量。
  - `[high]` `[patch]` WPF 启动改为异步初始化，避免同步阻塞 Dispatcher；存储不可用时仍建立托盘退出路径。
  - `[medium]` `[patch]` 仓储拒绝已关闭片段的开放写入，并补充相应 SQLite 回归测试。
  - `[medium]` `[patch]` 可执行文件身份的稳定键不再包含可变显示名，身份冲突时刷新安全元数据。
  - `[medium]` `[patch]` 补充显示名变化不分裂可执行身份的领域回归测试。

## Design Notes

协调器作为唯一片段写入者，将采样和生命周期输入归一化为领域状态转换；SQLite 仓储只落实原子存储约束。这使后续真实采样和会话/电源边界接入时不必绕过一致性规则，也避免 UI 直接操作数据库。

## Verification

**Commands:**
- `dotnet test FocusRecorder.sln` -- expected: 所有领域、应用、SQLite 与现有壳测试通过，数据均为合成。
- `dotnet build FocusRecorder.sln` -- expected: WPF 壳及所有分层项目使用 .NET 10 SDK 成功编译。

**Manual checks (if no CLI):**
- 检查实际 schema 仅有 migrations、segments、application identities、settings、structured logs，且持久化模型与日志字段没有标题、路径、内容或进程 ID。

## Auto Run Result

- 实现摘要：完成本地 SQLite 专注片段分层存储的复审修复，强化片段切换、关闭与 checkpoint 的一致性，稳定应用身份，并让 WPF 初始化失败时保持可退出。
- 变更文件：`RecordingCoordinator.cs` 修复状态切换和关闭失败后的调度恢复；`SqliteRecordingRepository.cs` 收紧开放片段与时间边界、刷新安全身份元数据；`FocusSegment.cs` 稳定可执行身份键；`App.xaml.cs` 异步启动并提供失败态托盘退出；领域和基础设施测试新增回归覆盖。
- 审查结果：已应用 8 项修复（高 5、中 3）；延期 0；驳回 12。
- 后续审查建议：true（本轮高严重度修复 5 项；中严重度 3 项、低严重度 0 项；评分 9）。
- 验证：`dotnet test FocusRecorder.sln` 通过（38 项）；`dotnet build FocusRecorder.sln` 通过，0 警告、0 错误。
- 残余风险：运行时真实或后续合成信号生产者仍属于后续采样故事；本故事仅提供其可调用的协调器与持久化边界。
