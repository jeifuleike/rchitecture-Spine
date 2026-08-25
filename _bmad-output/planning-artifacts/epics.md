---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-openHands-2026-08-26/prd.md
  - _bmad-output/planning-artifacts/prds/prd-openHands-2026-08-26/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-openHands-2026-08-26/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/ux-designs/ux-openHands-2026-08-26/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-openHands-2026-08-26/EXPERIENCE.md
---

# openHands - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for openHands, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: 专注记录器可以在 Windows 后台运行，并支持随系统启动自动开始记录；关闭主界面不停止记录，除非用户明确退出或暂停。

FR2: 系统必须按应用名粒度采样活跃应用，保存时间戳、应用名和稳定应用身份；默认采样间隔 5 秒，少于 15 秒的短暂切换默认合并回前后相同片段；不得保存截图、键盘输入、鼠标轨迹、窗口内容、网页标题、文档名、文件路径或聊天对象。

FR3: 系统必须识别空闲状态，默认空闲阈值 5 分钟且可配置；空闲、锁屏、睡眠不计入专注时长；恢复输入后从当前活跃应用开始新片段；跨自然日片段按本机时区拆分。

FR4: 系统必须完全无打扰运行，不主动弹出专注提醒、监督提示、确认问题、每日评价或打断式通知；错误或权限问题只能通过主界面或托盘被动展示。

FR5: 系统必须支持把应用身份映射到分类：编程、AI 对话、资料阅读、写作、沟通、娱乐/休闲、其他、未分类；用户可设置或修改分类；分类修改影响历史统计视图但不改写原始记录。

FR6: 系统必须按本机自然日展示应用和分类维度统计摘要，包括应用累计时长、分类累计时长、排名靠前的应用和分类、空闲时间、未分类应用；排序按专注时长降序并用名称打破并列；分类占比分母排除空闲和排除应用时间；少于 1 分钟可显示为“<1 分钟”。

FR7: 系统必须提供只在用户主动打开时出现的本地轻量报表界面，至少包含按应用统计、按分类统计和未分类应用列表，且不显示监督性文案。

FR8: v1 必须本地优先存储，不依赖后端即可离线记录、分类和报表；不得上传本地数据；用户可以删除本地数据；默认永久保留直到用户手动删除。

FR9: 未来如果加入后端同步，必须由用户显式开启，且同步范围只能是聚合统计摘要；不得同步原始片段、窗口标题、截图、键盘输入、文件路径或聊天内容。

FR10: 用户必须可以主动暂停和恢复记录；暂停后不生成新采样，恢复后从当前活跃应用开始新片段；暂停状态在主界面或托盘可见。

FR11: 用户必须可以添加、移除和查看排除应用；被排除应用活跃时不计入应用或分类统计，排除规则只保存在本地。

FR12: 用户必须可以删除某一天或全部本地数据，并导出 CSV 或 JSON 统计摘要；导出字段至少包含日期、分类、应用名、累计专注时长和空闲时长汇总；导出不包含原始活动记录且只由用户主动触发。

### NonFunctional Requirements

NFR1: 隐私边界必须强约束在应用名级别，不采集截图、键盘输入、鼠标轨迹、窗口内容、网页标题、文件路径、文档标题或聊天对象。

NFR2: v1 不包含后端、账号、遥测、HTTP 客户端或同步任务；数据库、设置、日志和导出保存在当前用户本地。

NFR3: 后台记录平均 CPU 占用应低于 1%，内存占用应低于 150 MB。

NFR4: 正常运行一天后，应用级统计不应丢失超过 1 分钟的连续前台应用活动；采样进程异常退出后应能在下次启动时继续记录。

NFR5: 报表必须能解释分类统计来源，至少能追溯到应用名级别。

NFR6: 产品不得转向监督、提醒、打卡、目标管理、强制复盘、生产力评分或网站拦截。

NFR7: v1 支持 Windows 11 22H2 及以上，以及当前 .NET 10 支持矩阵中的 Windows 10 LTSC/Enterprise；不支持管理员级自启动。

NFR8: 测试不得调用真实 Win32 API 或包含真实用户活动数据；领域与应用测试使用合成采样、会话和电源信号。

### Additional Requirements

- 使用分层单进程 WPF 桌面应用：App shell、Application services、Domain、Infrastructure。
- WPF 外壳承载主窗口和托盘；后台宿主与 UI 位于同一进程，关闭主窗口只隐藏 UI。
- `RecordingCoordinator` 是唯一能创建、关闭、拆分片段的组件，按单序列处理采样、空闲、锁屏、睡眠、暂停、恢复和跨日边界。
- Win32 适配器只能返回稳定应用身份、显示名、采样时刻和当前会话空闲时长；前台窗口使用 `GetForegroundWindow`，空闲检测使用 `GetLastInputInfo`。
- 隐藏消息窗口负责会话与电源边界，注册 `WTSRegisterSessionNotification` 并处理 `WM_WTSSESSION_CHANGE`、`WM_POWERBROADCAST`。
- SQLite 是唯一持久化事实源，保存专注片段、应用身份、分类规则、排除规则、设置和本地结构化日志。
- 本地数据默认保存在 `%LocalAppData%/FocusRecorder`。
- 稳定应用身份由应用显示名与可执行文件名或 Windows 包标识组成；进程 ID 和完整路径只能短暂用于 Win32 查询，不得持久化。
- 片段状态只能是 `focus`、`idle`、`excluded`、`paused`、`locked`、`sleeping`；只有 `focus` 计入专注总时长。
- `ApplicationIdentityId` 对 `focus` 片段必填，对 `idle` 和非专注状态为空；排除规则只影响未来信号。
- 开放片段创建时落盘，每 30 秒更新检查点结束时间；启动时遗留开放片段关闭到最后检查点。
- 所有片段边界以 UTC 保存，并在创建或拆分时写入当时的 `local_date` 和 UTC 偏移；报表、导出和单日删除按存储的 `local_date` 分组。
- 用户写命令必须在单一 SQLite 事务中完成；读取、报表和导出使用一致性快照。
- 报表查询是统计口径唯一实现，接收本地日期并返回应用行、分类行和未分类应用列表；UI 不重新计算统计。
- `Settings` 保存空闲阈值，允许范围 1-60 分钟；首次运行初始化默认分类；删除类别时把依赖规则改指向“未分类”。
- `SummaryExporter` 只能读取统计查询结果并写出 CSV 或 JSON 摘要，不得读取或导出原始片段。
- C# 命名使用 PascalCase；命令以 `Command` 结尾，查询以 `Query` 结尾，端口接口以 `I` 开头；SQLite 主键使用 UUID 文本，枚举以稳定字符串保存。

### UX Design Requirements

UX-DR1: 实现 DESIGN.md 中的颜色、字体、间距、圆角和组件 token，WPF 样式必须支持 `{colors.surface-base}`、`{colors.surface-panel}`、`{colors.primary}`、`{colors.focus-fill}`、`{colors.idle-fill}`、`{colors.unclassified-fill}`、`{colors.destructive}` 等语义用途。

UX-DR2: 主窗口必须以 Windows 工具型布局呈现：默认 960px+ 双栏，窄窗口时垂直堆叠；主要信息顺序固定为专注总时长、空闲时长、分类排行、应用排行、未分类应用。

UX-DR3: 今日报表表面必须支持日期选择，默认今天；切换日期只触发报表查询，不改写数据。

UX-DR4: 总览数字必须同时展示专注总时长与空闲时长，并明确空闲不计入专注总时长。

UX-DR5: 分类排行行必须显示分类名、累计时长、稳定占比条和排序结果；占比条只表达比例，不表达好坏。

UX-DR6: 应用排行行必须显示应用名、分类、累计专注时长，并提供修改分类或排除应用的行操作。

UX-DR7: 未分类列表必须只列未分类 focus 应用，每行提供分类下拉和保存动作；不得使用警告式文案。

UX-DR8: 设置表面必须提供空闲阈值、开机自启动、暂停/恢复、排除应用、数据删除和导出入口。

UX-DR9: 托盘菜单必须提供打开报表、暂停/恢复、状态查看和退出；正常记录状态不主动弹通知。

UX-DR10: 状态模式必须覆盖首次启动无数据、正在记录、已暂停、空闲中、锁屏/睡眠恢复、未分类过多、采集适配器失败、导出完成和删除完成。

UX-DR11: 微文案必须中性、短、可解释；不得出现“浪费”“高效”“低效”“失败”等监督或评价性措辞。

UX-DR12: 破坏性动作必须使用确认对话框，并在确认按钮或正文中点明删除范围；单日删除与全部删除必须分开。

UX-DR13: 所有控件必须可键盘访问，Tab 顺序与视觉阅读顺序一致；列表、下拉、按钮和托盘菜单项必须有清晰名称、角色和状态。

UX-DR14: 色彩不得作为唯一信息来源；专注、空闲、未分类必须同时使用文本标签。

UX-DR15: UI 不得展示窗口标题、文件路径、网页标题、聊天对象、截图、键盘输入或鼠标轨迹；导出界面只提供统计摘要。

### FR Coverage Map

FR1: Epic 1 - 安静后台记录与生命周期控制，包含后台运行、窗口关闭后继续记录和开机自启动开关。

FR2: Epic 1 - 活跃应用采样与稳定应用身份，包含 5 秒采样、短切换合并和禁止内容级采集。

FR3: Epic 1 - 空闲、锁屏、睡眠、恢复和跨日边界处理，确保非专注时段不污染专注统计。

FR4: Epic 1 - 无打扰运行与被动故障状态，确保正常记录不弹窗、不监督。

FR5: Epic 2 - 应用分类规则与历史统计重算，用户可整理未分类应用。

FR6: Epic 2 - 每日应用和分类统计摘要，包含排序、占比、空闲分离、未分类计入规则。

FR7: Epic 2 - 用户主动打开的本地轻量报表界面，包含应用统计、分类统计和未分类列表。

FR8: Epic 3 - 本地优先存储和删除控制，离线完整可用且不上传。

FR9: Epic 3 - 未来同步隐私边界，v1 不包含网络同步，后续同步只能 opt-in 且摘要级。

FR10: Epic 1 / Epic 3 - Epic 1 实现暂停/恢复对记录协调器的影响；Epic 3 完成设置和托盘中的用户控制体验。

FR11: Epic 3 - 排除应用列表，规则只保存在本地并只影响未来信号。

FR12: Epic 3 - 删除某日/全部数据与 CSV/JSON 摘要导出，不导出原始片段。

## Epic List

### Epic 1: 安静可靠的后台记录

用户可以安装并运行一个不会打扰他的 Windows 桌面工具；它在后台按应用级别记录真实活跃时间，识别空闲、锁屏、睡眠和暂停状态，并把片段可靠保存到本地 SQLite。完成后，即使主窗口关闭，系统也能持续积累可解释的本地记录。

**FRs covered:** FR1, FR2, FR3, FR4, FR10

**Implementation notes:** 建立 .NET 10 WPF 解决方案、单进程后台宿主、托盘入口、Win32 活跃应用与空闲适配器、会话/电源边界适配器、SQLite schema、`RecordingCoordinator`、片段检查点和基础状态展示。所有测试使用合成信号，不调用真实 Win32。

### Epic 2: 可解释的每日复盘与分类

用户主动打开报表后，可以按日期看到专注总时长、空闲时长、分类排行、应用排行和未分类应用，并能为应用设置分类；历史统计视图按当前分类规则重新计算，而原始记录保持不变。

**FRs covered:** FR5, FR6, FR7

**Implementation notes:** 实现报表查询模型、分类规则、默认类别初始化、未分类列表、WPF 报表 UI、UX token 样式、日期选择、行内分类动作、稳定排序和时长显示口径。UI 只渲染查询模型，不自行重算统计。

### Epic 3: 本地数据控制与隐私边界

用户能控制记录行为和本地数据：暂停/恢复、排除应用、调整空闲阈值、开启/关闭自启动、删除本地数据、导出统计摘要；v1 保持无网络、无账号、无上传，并把未来同步边界固定为显式开启的聚合摘要。

**FRs covered:** FR8, FR9, FR10, FR11, FR12

**Implementation notes:** 完成设置页、托盘控制、排除规则、空闲阈值设置、启动注册适配器、删除事务、CSV/JSON `SummaryExporter`、同库结构化日志和无 HTTP/遥测约束验证。

## Epic 1: 安静可靠的后台记录

用户可以安装并运行一个不会打扰他的 Windows 桌面工具；它在后台按应用级别记录真实活跃时间，识别空闲、锁屏、睡眠和暂停状态，并把片段可靠保存到本地 SQLite。完成后，即使主窗口关闭，系统也能持续积累可解释的本地记录。

### Story 1.1: 启动后保持后台宿主与托盘状态

As a Windows 个人用户,
I want 专注记录器启动后能驻留后台并通过托盘显示状态,
So that 我不需要保持主窗口打开也能持续记录。

**Requirements:** FR1, FR4, FR10, UX-DR9, UX-DR10

**Acceptance Criteria:**

**Given** 应用首次启动
**When** 主窗口初始化完成
**Then** 后台宿主开始运行，托盘入口可见
**And** 主窗口关闭只隐藏窗口，不停止后台宿主。

**Given** 应用正在记录
**When** 用户从托盘菜单选择“打开报表”
**Then** 主窗口显示或获得焦点
**And** 记录状态保持不变。

**Given** 应用正在运行
**When** 用户从托盘菜单选择“退出”
**Then** 后台宿主和记录协调器被有序停止
**And** 不留下新的开放片段。

### Story 1.2: 用本地 SQLite 保存最小专注片段

As a Windows 个人用户,
I want 应用把专注片段可靠保存在本地数据库,
So that 后续报表能从本地事实源解释我的一天。

**Requirements:** FR2, FR3, FR8, NFR2, NFR4

**Acceptance Criteria:**

**Given** 应用首次运行且本地数据库不存在
**When** 数据层初始化
**Then** `%LocalAppData%/FocusRecorder` 下创建 SQLite 数据库和所需 schema
**And** 本故事只创建迁移、片段、应用身份、设置和本地日志表；分类、排除和导出相关 schema 由后续需要它们的故事创建。

**Given** 记录协调器收到一个 focus 采样信号
**When** 该采样符合记录条件
**Then** 系统创建或延展一个 `focus` 片段
**And** 片段包含 UTC 边界、本地日期、UTC 偏移和稳定应用身份。

**Given** 系统处于 idle、paused、locked、sleeping 或 excluded 状态
**When** 记录协调器写入片段
**Then** 片段不保存应用身份
**And** 只有 `focus` 片段会被后续统计计入专注总时长。

### Story 1.3: 按应用名采样前台应用且不采集内容

As a Windows 个人用户,
I want 应用只识别当前活跃应用的稳定身份,
So that 我能获得应用级统计而不暴露窗口内容。

**Requirements:** FR2, NFR1, UX-DR15

**Acceptance Criteria:**

**Given** Win32 适配器读取前台窗口
**When** 它返回采样结果
**Then** 结果只包含采样时刻、应用显示名、可执行文件名或包标识、当前空闲时长
**And** 不返回窗口标题、文件路径、网页标题、文档名、聊天对象、键盘输入、鼠标轨迹或截图。

**Given** 用户在两个应用之间切换
**When** 采样周期到达默认 5 秒间隔
**Then** 后续采样记录新的稳定应用身份
**And** 稳定身份比较不依赖进程 ID。

**Given** 用户短暂切换到其他应用且少于 15 秒
**When** 前后稳定应用身份相同
**Then** 记录协调器将该短暂切换合并回同一专注片段
**And** 统计不出现噪声行。

### Story 1.4: 处理空闲、锁屏、睡眠、暂停和跨日边界

As a Windows 个人用户,
I want 应用自动切断不应计入专注的时间,
So that 离开电脑、锁屏和睡眠不会污染统计。

**Requirements:** FR3, FR4, NFR3, NFR4

**Acceptance Criteria:**

**Given** 用户超过空闲阈值没有键盘或鼠标输入
**When** 下一次协调器处理采样信号
**Then** 当前 focus 片段关闭，后续时间进入 `idle`
**And** 用户恢复输入后从当前活跃应用开始新 focus 片段。

**Given** Windows 会话进入锁屏或系统睡眠
**When** 会话/电源适配器发布边界信号
**Then** 当前片段被立即关闭
**And** 锁屏和睡眠时段分别写为 `locked` 或 `sleeping`，不计入专注总时长。

**Given** 一个片段跨越本地午夜
**When** 协调器处理日期边界
**Then** 片段按本地自然日拆分
**And** 每个片段保留创建时的本地日期和 UTC 偏移。

**Given** 应用异常退出后再次启动
**When** 仓储发现遗留开放片段
**Then** 它关闭到最后检查点结束时间
**And** 正常运行时开放片段至少每 30 秒检查点一次。

## Epic 2: 可解释的每日复盘与分类

用户主动打开报表后，可以按日期看到专注总时长、空闲时长、分类排行、应用排行和未分类应用，并能为应用设置分类；历史统计视图按当前分类规则重新计算，而原始记录保持不变。

### Story 2.1: 查询每日应用、分类与未分类统计

As a Windows 个人用户,
I want 按日期查询应用和分类统计,
So that 我能复盘某一天电脑时间主要花在哪里。

**Requirements:** FR5, FR6, UX-DR3, UX-DR4, UX-DR5

**Acceptance Criteria:**

**Given** 本地数据库存在 focus、idle 和 excluded 片段
**When** 报表查询请求某个 `local_date`
**Then** 返回应用行、分类行、未分类应用列表、专注总时长和空闲时长
**And** 只有 `focus` 片段进入应用与分类累计。

**Given** 多个应用或分类累计时长相同
**When** 查询返回排行
**Then** 按累计专注时长降序排列
**And** 并列时按名称升序排列。

**Given** 某应用没有分类规则
**When** 查询该日期统计
**Then** 该应用进入“未分类”分类并计入专注总时长
**And** 同时出现在未分类应用列表中。

### Story 2.2: 初始化默认分类并支持应用分类规则

As a Windows 个人用户,
I want 为应用设置或修改分类,
So that 历史报表能用我自己的分类理解时间分布。

**Requirements:** FR5, FR6

**Acceptance Criteria:**

**Given** 应用首次初始化分类数据
**When** 分类仓储建立默认分类
**Then** 分类和分类规则 schema 在迁移中创建，并存在编程、AI 对话、资料阅读、写作、沟通、娱乐/休闲、其他和未分类
**And** 未配置规则的应用默认映射到未分类。

**Given** 用户为某个稳定应用身份选择分类
**When** 保存分类规则
**Then** 规则以稳定应用身份为键保存在 SQLite
**And** 历史原始片段不被改写。

**Given** 用户修改已有分类规则
**When** 报表重新查询历史日期
**Then** 分类统计按当前规则重新计算
**And** 应用排行仍能解释分类来源。

### Story 2.3: 呈现轻量日报表界面

As a Windows 个人用户,
I want 主窗口呈现清楚的每日报表,
So that 我能主动打开后快速理解今天的分布。

**Requirements:** FR6, FR7, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR6

**Acceptance Criteria:**

**Given** 用户从托盘或应用打开主窗口
**When** 今日报表加载
**Then** 默认选中今天，并显示专注总时长、空闲时长、分类排行、应用排行和未分类应用
**And** 空闲时间单独展示，不混入专注总时长。

**Given** 用户切换日期
**When** 日期选择器变化
**Then** UI 只重新执行报表查询
**And** 不修改任何片段、分类规则或设置。

**Given** 窗口宽度低于 900px
**When** 报表重新布局
**Then** 导航和报表列表垂直堆叠
**And** 文本、按钮和占比条不互相遮挡。

### Story 2.4: 行内整理未分类应用

As a Windows 个人用户,
I want 直接从未分类列表给应用分类,
So that 报表能随着整理立即变得更可解释。

**Requirements:** FR5, FR6, FR7, UX-DR6, UX-DR7, UX-DR13

**Acceptance Criteria:**

**Given** 报表存在未分类应用
**When** 用户在未分类行选择一个分类并保存
**Then** 系统保存该应用身份的分类规则
**And** 当前报表原地刷新，未分类列表和分类排行同步更新。

**Given** 分类保存失败
**When** 用户尝试保存分类
**Then** 行内显示中性错误状态
**And** 不弹窗要求用户解释活动。

**Given** 用户使用键盘导航未分类列表
**When** 焦点进入分类下拉和保存按钮
**Then** 控件名称、角色和状态可被辅助技术识别
**And** Tab 顺序与视觉阅读顺序一致。

### Story 2.5: 应用 UX 视觉规范和中性文案

As a Windows 个人用户,
I want 报表看起来像一个克制的本地工具,
So that 复盘时看到的是事实而不是评价。

**Requirements:** FR4, FR7, UX-DR1, UX-DR10, UX-DR11, UX-DR14

**Acceptance Criteria:**

**Given** WPF 样式初始化
**When** 主窗口渲染报表、设置和行内操作
**Then** 使用 DESIGN.md 中的语义颜色、Segoe UI 字体、4px 间距节奏和最大 8px 圆角
**And** 专注、空闲、未分类除颜色外都有文本标签。

**Given** 报表、设置或错误状态显示文案
**When** 用户阅读界面
**Then** 文案保持中性、短、可解释
**And** 不出现“浪费”“低效”“失败”“高效”等监督性措辞。

**Given** 用户开启 Windows 文本缩放
**When** 主窗口显示主要报表和操作按钮
**Then** 文本仍在容器内可读
**And** 交互目标不小于 32px。

## Epic 3: 本地数据控制与隐私边界

用户能控制记录行为和本地数据：暂停/恢复、排除应用、调整空闲阈值、开启/关闭自启动、删除本地数据、导出统计摘要；v1 保持无网络、无账号、无上传，并把未来同步边界固定为显式开启的聚合摘要。

### Story 3.1: 设置和托盘中的记录控制

As a Windows 个人用户,
I want 在托盘和设置里控制记录状态与基础行为,
So that 静默记录仍然可见、可暂停、可恢复。

**Requirements:** FR1, FR3, FR4, FR10, UX-DR8, UX-DR9, UX-DR10

**Acceptance Criteria:**

**Given** 用户从托盘菜单或设置页选择暂停
**When** 命令被处理
**Then** 记录协调器停止生成新的活动采样片段并写入 `paused` 状态
**And** 托盘和设置显示已暂停。

**Given** 用户从托盘菜单或设置页选择恢复
**When** 命令被处理
**Then** 记录协调器从当前活跃应用开始新片段
**And** 托盘和设置显示正在记录。

**Given** 用户修改空闲阈值
**When** 输入值在 1-60 分钟范围内并保存
**Then** 设置写入 SQLite
**And** 新阈值从协调器收到的下一信号生效。

**Given** 用户启用或关闭开机自启动
**When** 启动注册适配器执行命令
**Then** 只写入或删除当前用户 Run 注册值
**And** 失败只在设置或托盘状态被动展示。

### Story 3.2: 管理排除应用列表

As a Windows 个人用户,
I want 把指定应用排除在统计之外,
So that 私人或无关应用不会进入专注报表。

**Requirements:** FR11, UX-DR6, UX-DR8

**Acceptance Criteria:**

**Given** 用户从应用排行行选择排除某应用
**When** 排除命令保存
**Then** 排除规则 schema 在迁移中创建，并以稳定应用身份为键写入本地 SQLite
**And** 规则只影响未来采样。

**Given** 被排除应用成为活跃应用
**When** 协调器处理采样信号
**Then** 该时段写为不带应用身份的 `excluded` 片段
**And** 不进入应用统计或分类统计。

**Given** 用户移除某排除规则
**When** 后续采样再次出现该应用
**Then** 新时段可按 focus 记录
**And** 历史 excluded 时段不恢复为应用时间。

### Story 3.3: 删除本地数据并保持事务一致

As a Windows 个人用户,
I want 删除某一天或全部本地数据,
So that 我能控制本机保存的记录范围。

**Requirements:** FR8, FR12, UX-DR8, UX-DR10, UX-DR12

**Acceptance Criteria:**

**Given** 用户选择删除某一天的数据
**When** 确认对话框显示
**Then** 对话框明确写出该本地日期和只删除本地记录
**And** 确认后只删除该 `local_date` 的片段，不删除分类规则或设置。

**Given** 用户选择删除全部本地数据
**When** 确认删除
**Then** 单一 SQLite 事务删除片段、规则、分类、设置和本地日志
**And** 保留可重新初始化的数据库文件。

**Given** 删除操作完成
**When** 当前报表刷新
**Then** UI 显示对应空状态或剩余日期数据
**And** 不触发任何上传或外部同步。

### Story 3.4: 导出 CSV/JSON 统计摘要

As a Windows 个人用户,
I want 导出本地统计摘要,
So that 我能备份或检查聚合后的时间分布。

**Requirements:** FR8, FR12, UX-DR8, UX-DR15

**Acceptance Criteria:**

**Given** 用户选择某个日期并选择 CSV 或 JSON 导出
**When** `SummaryExporter` 执行
**Then** 导出文件只包含日期、分类、应用名、累计专注时长和空闲时长汇总
**And** 不包含原始片段、窗口标题、文件路径、网页标题、聊天对象、截图、键盘输入或鼠标轨迹。

**Given** 导出成功
**When** UI 更新状态
**Then** 原地显示完成状态和本地文件路径
**And** 不自动上传、不打开远程服务。

**Given** 导出失败
**When** 文件写入抛出错误
**Then** UI 显示中性错误文案
**And** 原始数据保持不变。

### Story 3.5: 固化无网络与未来同步边界

As a Windows 个人用户,
I want v1 明确保持本地优先且没有上传通道,
So that 我能信任记录不会离开当前电脑。

**Requirements:** FR8, FR9, NFR1, NFR2, UX-DR11, UX-DR15

**Acceptance Criteria:**

**Given** v1 应用项目和依赖清单
**When** 构建和测试检查运行
**Then** 不存在 HTTP 客户端、账号、遥测或同步任务依赖
**And** 没有后台网络上传代码路径。

**Given** UI 展示数据控制或导出功能
**When** 用户查看相关文案
**Then** 界面只描述本地删除和统计摘要导出
**And** 不出现登录、账号、同步或上传入口。

**Given** 后续版本要加入同步
**When** 规划引用 v1 边界
**Then** 必须保留显式 opt-in 和聚合摘要限定
**And** 原始片段和被禁字段永远不得成为同步载荷。
