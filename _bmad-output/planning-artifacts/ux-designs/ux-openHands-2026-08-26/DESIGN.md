---
name: 专注记录器
description: Windows 本地个人专注统计工具。安静、克制、解释清楚，不监督用户。
status: final
created: 2026-08-26
updated: 2026-08-26
sources:
  - ../../prds/prd-openHands-2026-08-26/prd.md
  - ../../architecture/architecture-openHands-2026-08-26/ARCHITECTURE-SPINE.md
colors:
  surface-base: '#F7F7F2'
  surface-panel: '#FFFFFF'
  surface-subtle: '#ECEFE8'
  ink-primary: '#1E2320'
  ink-secondary: '#5E675F'
  ink-muted: '#7D867E'
  border-hairline: '#D8DED6'
  primary: '#1F7A68'
  primary-foreground: '#FFFFFF'
  focus-fill: '#DDEFEA'
  idle-fill: '#E8E2D7'
  unclassified-fill: '#F3E0DA'
  destructive: '#B94134'
  destructive-foreground: '#FFFFFF'
  info: '#586B8A'
typography:
  title:
    fontFamily: 'Segoe UI'
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.25'
    letterSpacing: 0
  section:
    fontFamily: 'Segoe UI'
    fontSize: 16px
    fontWeight: '600'
    lineHeight: '1.35'
    letterSpacing: 0
  body:
    fontFamily: 'Segoe UI'
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.45'
    letterSpacing: 0
  label:
    fontFamily: 'Segoe UI'
    fontSize: 12px
    fontWeight: '600'
    lineHeight: '1.35'
    letterSpacing: 0
  mono:
    fontFamily: 'Cascadia Mono'
    fontSize: 12px
    fontWeight: '400'
    lineHeight: '1.35'
    letterSpacing: 0
rounded:
  sm: 4px
  md: 6px
  lg: 8px
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 20px
  '6': 24px
  '8': 32px
  panel-gap: 12px
  page-margin: 20px
components:
  primary-button:
    background: '{colors.primary}'
    foreground: '{colors.primary-foreground}'
    radius: '{rounded.md}'
  secondary-button:
    background: '{colors.surface-panel}'
    foreground: '{colors.ink-primary}'
    border: '{colors.border-hairline}'
    radius: '{rounded.md}'
  report-row:
    background: '{colors.surface-panel}'
    foreground: '{colors.ink-primary}'
    border: '{colors.border-hairline}'
    radius: '{rounded.sm}'
  focus-bar:
    background: '{colors.focus-fill}'
    accent: '{colors.primary}'
  idle-bar:
    background: '{colors.idle-fill}'
    accent: '{colors.ink-secondary}'
  unclassified-row:
    background: '{colors.unclassified-fill}'
    foreground: '{colors.ink-primary}'
---

# 专注记录器 - Design Spine

## Brand & Style

专注记录器是一个安静的个人工具，不是效率教练、打卡器或监督者。视觉上应像 Windows 上可靠的小型系统工具：清楚、轻、少装饰，在用户主动打开时把事实摆好，然后退后。

设计语言是“克制的本地仪表盘”。它允许密集信息，但不能像企业监控面板；允许颜色帮助区分专注、空闲和未分类，但颜色不评价用户行为。所有视觉选择服务于复盘、解释和控制权。

## Colors

- **Quiet Canvas `{colors.surface-base}`** 是主窗口背景。它比纯白更柔和，适合晚上复盘。
- **Panel White `{colors.surface-panel}`** 用于列表、设置区和查询结果。它表达信息层级，不表达奖励或惩罚。
- **Ink `{colors.ink-primary}` / Secondary Ink `{colors.ink-secondary}`** 承担主要文本和辅助文本。
- **Teal `{colors.primary}`** 只表示可执行的主要控制，例如恢复记录、保存分类、导出摘要。它不代表“表现好”。
- **Focus Fill `{colors.focus-fill}`** 表示计入专注总时长的片段或占比条背景。
- **Idle Fill `{colors.idle-fill}`** 表示空闲/未计入专注。它必须明显区别于专注，但不能像警告。
- **Unclassified Fill `{colors.unclassified-fill}`** 提醒用户有可整理项，但语气应保持中性。
- **Destructive `{colors.destructive}`** 只用于删除某日数据、删除全部数据等不可逆确认。

避免：红绿成绩感、生产力评分色、奖励色、渐变背景、彩色徽章泛滥。

## Typography

界面继承 Windows / WPF 的 Segoe UI。字号层级紧凑，适合工具型窗口：标题 `{typography.title}`，分区标题 `{typography.section}`，正文和表格行 `{typography.body}`，小标签 `{typography.label}`。数字和导出预览可使用 `{typography.mono}`。

不要使用超大展示字、全大写标签或负字距。统计数字可以加粗，但不应做成英雄式战报。

## Layout & Spacing

布局以单窗口、双栏为默认：左侧日期和导航，右侧当前日期报表。主内容最小宽度按 960px 设计，窄窗口时垂直堆叠。间距使用 4px 基础节奏：局部控件 `{spacing.2}`，列表行内部 `{spacing.3}`，面板间 `{spacing.panel-gap}`，窗口边距 `{spacing.page-margin}`。

报表优先列表和基础占比条，不使用复杂图表。主要信息顺序固定：专注总时长、空闲时长、分类排行、应用排行、未分类应用。这样用户可以从概览走向解释。

## Elevation & Depth

深度主要靠色块和 1px 边线，不靠阴影。常驻窗口、设置面板和列表行使用 `{colors.border-hairline}` 分隔。只有浮层菜单、确认对话框和托盘上下文菜单可以有系统默认阴影。

## Shapes

圆角保持工具感：小控件 `{rounded.sm}`，按钮和输入 `{rounded.md}`，对话框 `{rounded.lg}`。卡片和面板不得超过 8px 圆角。避免大药丸形容器；开关控件可以遵循 Windows 原生形态。

## Components

- **主按钮** 使用 `{components.primary-button}`，只用于保存、恢复、导出等明确动作。
- **次按钮** 使用 `{components.secondary-button}`，用于暂停、取消、打开设置、切换日期。
- **报表行** 使用 `{components.report-row}`，行高稳定，左侧名称，右侧时长和占比。
- **分类占比条** 背景按分类状态使用 `{components.focus-bar}`、`{components.idle-bar}` 或 `{components.unclassified-row}`，条形只表达比例，不表达好坏。
- **未分类应用行** 使用 `{components.unclassified-row}`，右侧提供分类下拉，不使用警告图标。
- **删除确认** 使用 `{colors.destructive}`，按钮文案必须点明删除范围。
- **托盘状态** 使用系统托盘图标和简短状态文本；正常记录状态不主动弹通知。

## Do's and Don'ts

| Do | Don't |
| --- | --- |
| 用中性文案呈现事实 | 写“浪费”“低效”“失败”等判断 |
| 列表和占比条优先 | 在 v1 做复杂图表或时间线 |
| 把空闲时间单独展示 | 把空闲混入专注总时长 |
| 让删除、暂停、排除有明确控制 | 把静默记录做成不可见、不可控 |
| 用颜色解释状态 | 用颜色评价用户 |
| 保持 Windows 工具感 | 做营销页、成就页或打卡界面 |
