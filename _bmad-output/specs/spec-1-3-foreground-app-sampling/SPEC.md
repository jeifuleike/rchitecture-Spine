---
id: SPEC-1-3-foreground-app-sampling
companions:
  - sampling-contract.md
sources:
  - ../../planning-artifacts/epics.md
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract for what to build, test, and validate. Source documents listed in frontmatter are for traceability — consult them only if you need narrative rationale or prose color this contract intentionally omits.

# Story 1.3：前台应用级采样与隐私边界

## Why

这是一个隐私与可解释性兼具的记录能力：个人用户需要知道时间花在哪些应用上，但不应为了统计而暴露窗口、文件、网页或输入内容。现在必须先把可采集数据和不可采集数据固定为契约，避免后续实现把应用级记录扩展成内容监控。

## Capabilities

- **CAP-1**
  - **intent:** 系统可以按默认 5 秒间隔获取当前前台应用的最小化采样信号，以支持应用级时间记录。
  - **success:** 每个采样只包含采样时刻、显示名、稳定应用身份所需的可执行文件名或包标识，以及当前会话空闲时长。
- **CAP-2**
  - **intent:** 系统可以跨进程运行稳定识别同一应用，以便后续片段、分类和统计使用一致身份。
  - **success:** 相同应用的身份比较不依赖进程 ID；包标识可用时优先使用，否则使用规范化显示名和小写可执行文件名。
- **CAP-3**
  - **intent:** 系统可以抑制极短应用切换造成的统计噪声。
  - **success:** 当短暂切换少于 15 秒且前后稳定应用身份相同，协调器将其合并，不产生独立统计行。

## Constraints

- 采集和持久化禁止窗口标题、文件路径、网页或文档内容、聊天对象、键盘输入、鼠标轨迹、截图和进程 ID。
- 采集适配器只发布信号；`RecordingCoordinator` 独占片段的创建、关闭、拆分和合并。
- 当前范围只产出规格文档，不创建应用代码或测试代码。

## Non-goals

- 不识别浏览器标签、网页地址、文档标题或应用内活动。
- 不实现空闲、锁屏、睡眠、暂停或跨日边界处理。
- 不定义分类、报表、导出或网络同步能力。

## Success signal

- 审阅者能够用采样契约证明：任一采样和稳定身份都不含内容级或路径级数据，同时能区分应用切换并合并满足条件的短暂切换。

