---
title: 'Story 1.1: 启动后保持后台宿主与托盘状态'
type: 'feature'
created: '2026-08-26'
status: 'review'
review_loop_iteration: 0
followup_review_recommended: false
baseline_commit: '906b95a5e6e3543d1005f0faf6f918ff88c8fcc9'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** 仓库目前没有应用源码，Story 1.1 需要建立专注记录器的最小 Windows 桌面骨架。这个骨架必须证明应用可以启动、保持后台宿主、通过托盘展示被动状态，并在主窗口关闭后继续运行。

**Approach:** 创建 .NET/WPF 应用项目和最小 shell：App composition root、MainWindow、BackgroundHostService、TrayController 和记录状态模型。先用内存状态证明生命周期和托盘交互，不引入 SQLite、Win32 采样或会话/电源边界。

## Boundaries & Constraints

**Always:** 单进程；关闭主窗口只隐藏窗口；显式退出才停止后台宿主；托盘状态被动展示；正常运行不弹通知；文案中性；项目结构必须为后续 Application/Domain/Infrastructure 分层留出路径。

**Ask First:** 如果需要安装系统级 .NET SDK、引入第三方托盘库、改变架构指定的 .NET 10/WPF 目标、或把 Story 1.2+ 的持久化/采样功能提前做进本故事，必须先问用户。

**Never:** 不实现 SQLite；不实现真实 Win32 前台应用采样；不读取窗口标题、路径、URL、键盘、鼠标或截图；不添加账号、网络、遥测、同步、评分、提醒或生产力评价。

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Start app | 用户启动 WPF 应用 | 后台宿主进入 Running，托盘图标存在，主窗口可显示状态 | 初始化异常显示为被动状态文本，不弹通知 |
| Close main window | 用户点击窗口关闭按钮 | 窗口隐藏，后台宿主仍 Running，托盘仍可打开窗口 | 如果隐藏失败，允许正常关闭但不崩溃 |
| Open from tray | 用户点击托盘“打开报表” | 主窗口显示并获得焦点，后台宿主状态不变 | 如果窗口不存在则重建主窗口 |
| Exit from tray | 用户点击托盘“退出” | 执行有序退出，后台宿主停止，托盘图标释放 | 重复退出必须幂等 |

</intent-contract>

## Code Map

- `D:\project-sc\rchitecture-Spine\src\FocusRecorder.App\` -- 已存在但为空的应用层目录；本故事在其中建立 WPF 壳和 shell 服务。
- `D:\project-sc\rchitecture-Spine\src\FocusRecorder.Application\`、`Domain\`、`Infrastructure\` -- 已预建为空目录，仅作为后续故事的分层边界；本故事不得向其中加入采样或持久化实现。
- `D:\project-sc\rchitecture-Spine\tests\` -- 已预建三个空测试项目目录；为可在无 WPF 图形会话下验证的后台宿主状态机添加单元测试项目。
- `D:\project-sc\rchitecture-Spine\_bmad-output\implementation-artifacts\epic-1-context.md` -- 已重新生成的 Epic 1 约束：单实例、关闭隐藏、托盘被动入口、显式退出及无敏感采集。
- `D:\project-sc\rchitecture-Spine\_bmad-output\implementation-artifacts\sprint-status.yaml` -- 完成实现后将 `epic-1` 置为 `in-progress`，将本故事置为 `review`；不要改动其他故事。
- 环境检查：工作树基线为 `906b95a5e6e3543d1005f0faf6f918ff88c8fcc9`，分支为 `main`；已安装 .NET 10 SDK，并已通过自动化构建与测试。

## Tasks & Acceptance

**Execution:**
- `D:\project-sc\rchitecture-Spine\FocusRecorder.sln` -- 创建包含 App 与测试项目的 solution 入口，维持未来分层项目可加入的结构。
- `D:\project-sc\rchitecture-Spine\src\FocusRecorder.App\FocusRecorder.App.csproj`、`App.xaml`、`App.xaml.cs` -- 创建 `net10.0-windows` WPF 可执行项目；组合根负责单实例协调、后台宿主和托盘控制器的生命周期，并将未处理初始化错误降级为主窗口中的被动状态。
- `D:\project-sc\rchitecture-Spine\src\FocusRecorder.App\MainWindow.xaml`、`MainWindow.xaml.cs` -- 提供只呈现当前记录状态的中性主窗口；拦截用户关闭以隐藏窗口，且提供由托盘打开时的显示/激活入口。
- `D:\project-sc\rchitecture-Spine\src\FocusRecorder.App\Services\RecordingStatus.cs`、`BackgroundHostService.cs` -- 定义最小状态枚举/快照和线程安全、幂等的 Start/Stop 生命周期服务；不连接数据库或真实系统采样。
- `D:\project-sc\rchitecture-Spine\src\FocusRecorder.App\Services\TrayController.cs`、`SingleInstanceCoordinator.cs` -- 以框架自带托盘 API 创建被动图标与“打开报表 / 状态 / 退出”菜单，释放图标资源；用当前用户会话命名的互斥体及唤醒信号把第二实例转发到现有窗口后退出。
- `D:\project-sc\rchitecture-Spine\tests\FocusRecorder.App.Tests\FocusRecorder.App.Tests.csproj`、`BackgroundHostServiceTests.cs` -- 以合成调用验证宿主启动、停止、重复停止以及状态变更；不启动 WPF、托盘或真实 Win32 API。
- `D:\project-sc\rchitecture-Spine\_bmad-output\implementation-artifacts\sprint-status.yaml` -- 在全部源码和测试文件写入后，仅更新 Epic 1 与 Story 1.1 的状态为 `in-progress` / `review`。

**Acceptance Criteria:**
- Given 首个应用实例已启动，when 组合根初始化完成，then 后台宿主处于 `Running`、托盘入口可用，且不会显示通知或模态提示。
- Given 后台宿主正在运行，when 用户关闭主窗口，then 窗口被隐藏而后台宿主和托盘保持可用。
- Given 应用正在记录，when 用户从托盘选择“打开报表”，then 主窗口显示并获得焦点，记录状态不改变。
- Given 应用正在运行，when 用户从托盘选择“退出”或重复触发退出，then 宿主恰好停止一次、托盘资源被释放、进程可正常结束。
- Given 同一 Windows 用户会话已有运行实例，when 第二实例启动，then 第二实例请求原实例显示窗口并自行退出，不创建第二个后台宿主或托盘图标。
- Given 正常运行或可恢复的初始化异常，when 主窗口或托盘呈现状态，then 文案为中性被动描述，不包含提醒、评分、监督或活动内容。

## Spec Change Log

## Review Triage Log

## Design Notes

以 `BackgroundHostService` 作为没有外部副作用的生命周期接缝：本故事只验证宿主边界，Story 1.2+ 可在其后接入协调器、SQLite 与 Win32 适配器。托盘采用框架自带 API，避免引入第三方依赖。

## Verification

**Commands:**
- `dotnet test D:\project-sc\rchitecture-Spine\FocusRecorder.sln` -- expected: 安装 .NET 10 SDK 的 Windows 环境中，生命周期单元测试全部通过。
- `dotnet build D:\project-sc\rchitecture-Spine\FocusRecorder.sln` -- expected: WPF 应用和测试项目编译成功，且不还原第三方托盘依赖。

**Manual checks (if no CLI):**
- 检查项目文件目标为 `net10.0-windows` 并启用 WPF；检查关闭事件只隐藏窗口，显式退出路径按“停止宿主、释放托盘、关闭应用”顺序执行。
- 在具备 Windows 图形会话的 .NET 10 SDK 环境中启动应用，逐项执行矩阵中的启动、关闭、托盘打开、退出和第二实例行为。

## Auto Run Result

Status: review
Automated verification: 已安装 .NET 10 SDK 10.0.400，并通过 `dotnet test FocusRecorder.sln --no-restore`（5/5）与 `dotnet build FocusRecorder.sln --no-restore`（0 warnings, 0 errors）；`git diff --check` 通过。托盘、窗口关闭隐藏和第二实例行为仍留待 Windows 图形会话人工验收。
