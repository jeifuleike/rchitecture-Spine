# PRD Addendum: 专注记录器

## 轻量研究摘要

本摘要用于支撑 PRD 范围判断，不作为架构决策。

- 同类工具通常围绕后台自动记录前台应用、活动时间段和空闲状态构建。
- 对本产品的 MVP 来说，“活跃应用采样 + 空闲检测”已经足够形成可用统计，不需要手动计时器或内容级分析。
- 隐私友好边界应明确排除键盘输入、截图、录屏、窗口内容、网页标题、文件路径和聊天内容。
- “只按应用名粒度统计”比许多同类工具更克制，应作为产品原则写清楚。
- 本地优先是重要信任点；未来后端同步也应限制在聚合统计摘要。
- 空闲检测是必要能力，否则离开电脑、锁屏、睡眠等状态会污染专注统计。
- MVP 值得保留的用户控制包括暂停/恢复记录、排除应用、删除本地数据、导出数据。
- MVP 应避免目标提醒、番茄钟、网站拦截、AI 教练、团队看板、项目归因、账单工时和云同步。

## 实现提示（非 PRD 需求）

- 本地存储可考虑 SQLite，便于按日期、应用和分类汇总。
- 最小事件字段可包括 `app_name`、`executable_name` 或 `package_id`、`started_at`、`ended_at`、`duration`、`idle_state`。避免把 Windows `process_id` 作为长期分类键。
- 可把“活动采样”和“空闲检测”视作两个独立采集器，后续由统计层合并。

## 参考来源

- [ActivityWatch Privacy](https://docs.activitywatch.net/en/latest/privacy.html)
- [ActivityWatch Watchers](https://docs.activitywatch.net/en/latest/watchers.html)
- [ActivityWatch Data](https://docs.activitywatch.net/en/latest/examples/working-with-data.html)
- [ManicTime Tracking](https://docs.manictime.com/win-client/tracking)
- [ManicTime Tracking Settings](https://docs.manictime.com/win-client/settings/tracking)
- [ManicTime Database Location](https://docs.manictime.com/win-client/faq/database-location)
- [RescueTime Privacy](https://www.rescuetime.com/privacy)
- [RescueTime Data Controls](https://help.rescuetime.com/article/70-can-i-limit-what-information-rescuetime-collects)
- [Rize Privacy and Security](https://rize.io/guides/privacy-and-security)
