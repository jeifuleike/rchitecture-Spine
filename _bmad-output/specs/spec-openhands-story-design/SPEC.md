---
id: SPEC-openhands-story-design
companions:
  - story-design-catalog.md
  - ../../planning-artifacts/architecture/architecture-openHands-2026-08-26/ARCHITECTURE-SPINE.md
  - ../../planning-artifacts/ux-designs/ux-openHands-2026-08-26/DESIGN.md
  - ../../planning-artifacts/ux-designs/ux-openHands-2026-08-26/EXPERIENCE.md
sources:
  - ../../planning-artifacts/epics.md
---

> **Canonical contract.** This SPEC and the files in `companions:` define the shared, preservation-validated contract for Story-level planning; each Story's later implementation specification derives from this contract. No application code is in scope.

# openHands 全量 Story 设计

## Why

项目已具备产品、架构和 UX 方向，但需要一份能让后续团队按依赖顺序取用的完整 Story 设计包。该设计以本地优先和应用级隐私为前提，先固定每个可审阅交付的边界，避免文档规划被实现细节或监督型功能稀释。

## Capabilities

- **CAP-1**
  - **intent:** 团队可以按 Epic 1 的故事顺序设计可靠、隐私受限的后台记录基础。
  - **success:** 四个故事分别覆盖生命周期、最小 SQLite 事实源、前台应用级采样和状态边界，且其依赖关系明确。
- **CAP-2**
  - **intent:** 团队可以按 Epic 2 的故事顺序设计可解释的日报表与分类体验。
  - **success:** 五个故事完整覆盖统计口径、分类规则、报表表面、未分类整理与中性可访问 UX。
- **CAP-3**
  - **intent:** 团队可以按 Epic 3 的故事顺序设计本地数据控制与隐私边界。
  - **success:** 五个故事完整覆盖记录控制、排除、删除、摘要导出和无网络边界。

## Constraints

- 所有故事继承本地优先、无账号/遥测/网络上传、应用名级采集和中性无打扰体验。
- `RecordingCoordinator` 独占片段状态；SQLite 是记录事实唯一来源；UI 不自行重算统计。
- 当前交付仅限文档；所有故事均保持 `backlog` 状态，不生成代码、测试或可执行项目。

## Non-goals

- 不调整已批准的 PRD、架构或 UX 方向。
- 不把故事推进为实现、审查或完成状态。
- 不加入同步、账号、生产力评分、提醒或内容级监控。

## Success signal

- 任一 Story 都能从目录直接找到设计输入、用户价值、依赖、范围与验收焦点；团队能按 1.1 至 3.5 的顺序派生独立实施规格而无需重新拆解需求。
