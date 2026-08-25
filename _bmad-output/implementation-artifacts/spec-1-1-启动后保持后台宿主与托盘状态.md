---
title: 'Story 1.1: 启动后保持后台宿主与托盘状态'
type: 'feature'
created: '2026-08-26'
status: 'in-progress'
review_loop_iteration: 0
story_key: '1-1-启动后保持后台宿主与托盘状态'
baseline_commit: 'NO_VCS'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

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

</frozen-after-approval>

## Code Map

- `D:\ai\openHands` -- 当前没有应用源码、solution、csproj 或 C# 文件；本故事是 greenfield skeleton。
- `D:\ai\openHands\_bmad-output\implementation-artifacts\epic-1-context.md` -- Epic 1 的实现上下文，包含生命周期、托盘、无打扰和后续故事边界。
- `D:\ai\openHands\_bmad-output\planning-artifacts\epics.md` -- Story 1.1 验收标准和 FR/UX 追踪来源。
- `D:\ai\openHands\_bmad-output\implementation-artifacts\sprint-status.yaml` -- 当前 story key 为 `1-1-启动后保持后台宿主与托盘状态`，实现完成后需要推进到 `review`。
- `dotnet --info` -- 当前机器只有 .NET runtime，没有 SDK；实现可以写文件，但本机无法执行 `dotnet build`，除非用户安装 SDK。

## Tasks & Acceptance

**Execution:**
- [ ] `D:\ai\openHands\FocusRecorder.sln` -- 创建 solution 壳 -- 为后续多项目结构提供入口。
- [ ] `D:\ai\openHands\src\FocusRecorder.App\FocusRecorder.App.csproj` -- 创建 WPF exe 项目，目标 `net10.0-windows`，启用 WPF 和 Windows targeting -- 对齐架构栈。
- [ ] `D:\ai\openHands\src\FocusRecorder.App\App.xaml` and `App.xaml.cs` -- 实现 composition root、启动后台宿主、创建主窗口和托盘控制器 -- 保证单进程生命周期。
- [ ] `D:\ai\openHands\src\FocusRecorder.App\MainWindow.xaml` and `MainWindow.xaml.cs` -- 实现最小状态窗口，关闭时隐藏而不退出 -- 满足 Story 1.1 用户可见行为。
- [ ] `D:\ai\openHands\src\FocusRecorder.App\Services\BackgroundHostService.cs` -- 实现幂等 Start/Stop 和状态变更事件 -- 后续记录协调器可挂接。
- [ ] `D:\ai\openHands\src\FocusRecorder.App\Services\TrayController.cs` -- 实现托盘图标、打开报表、状态项和退出命令，并确保释放资源 -- 满足托盘交互。
- [ ] `D:\ai\openHands\src\FocusRecorder.App\Services\RecordingStatus.cs` -- 定义最小状态模型 -- 避免 UI 直接持有散乱字符串。
- [ ] `D:\ai\openHands\_bmad-output\implementation-artifacts\sprint-status.yaml` -- 将当前故事推进到 `review` -- 同步 BMad sprint 状态。

**Acceptance Criteria:**
- Given 应用首次启动, when 主窗口初始化完成, then 后台宿主开始运行，托盘入口可见。
- Given 应用正在运行, when 用户关闭主窗口, then 窗口隐藏且后台宿主保持 Running。
- Given 应用正在记录, when 用户从托盘菜单选择“打开报表”, then 主窗口显示或获得焦点且记录状态保持不变。
- Given 应用正在运行, when 用户从托盘菜单选择“退出”, then 后台宿主和托盘控制器有序停止并释放资源。
- Given 正常运行状态, when UI 或托盘展示文案, then 文案保持中性且不出现提醒、评分或监督语气。

## Spec Change Log

## Design Notes

本故事故意使用内存中的 `BackgroundHostService`，不引入真正采样或持久化。这样 Story 1.1 可以独立完成生命周期价值，同时为 Story 1.2 的 SQLite 和 Story 1.3 的 Win32 采样留出清晰接缝。

## Verification

**Commands:**
- `dotnet build D:\ai\openHands\FocusRecorder.sln` -- expected: 在安装 .NET 10 SDK 的环境中编译成功。

**Manual checks (if no CLI):**
- 当前机器缺 .NET SDK 时，检查 project/source 文件存在，WPF lifecycle 代码路径满足关闭隐藏、托盘打开、托盘退出和资源释放。
