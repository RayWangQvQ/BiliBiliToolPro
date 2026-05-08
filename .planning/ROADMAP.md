# Roadmap: BiliBiliToolPro Brownfield Refactor

## Milestones

- 🔲 **v4.0.0.8 Notification Boundary** — Phases 20–23
- ✅ **v4.0.0.7 Bili Account Management** — Phases 17–19 (shipped 2026-05-07) — [archive](milestones/v4.0.0.7-ROADMAP.md)
- ✅ **v4.0.0.6 Web Layer Boundary Cleanup** — Phases 13–16 (shipped 2026-05-05) — [archive](milestones/v4.0.0.6-ROADMAP.md)
- ✅ **v4.0.0.1 Brownfield Refactor** — Phases 1–6 (shipped 2026-05-03) — [archive](milestones/v4.0.0.1-ROADMAP.md)
- ✅ **v4.0.0.2 AppService Refactor Continuation** — Phase 7 (shipped 2026-05-04) — [archive](milestones/v4.0.0.2-ROADMAP.md)
- ✅ **v4.0.0.3 Refit Migration** — Phases 8–10 (shipped 2026-05-04) — [archive](milestones/v4.0.0.3-ROADMAP.md)
- ✅ **v4.0.0.4 Agent Interface Consolidation** — Phase 11 (shipped 2026-05-04) — [archive](milestones/v4.0.0.4-ROADMAP.md)
- ✅ **v4.0.0.5 Agent DTO Reorganization** — Phase 12 (shipped 2026-05-04) — [archive](milestones/v4.0.0.5-ROADMAP.md)

## Phases

<details open>
<summary>🔲 v4.0.0.8 Notification Boundary (Phases 20–23) — IN PROGRESS</summary>

- [x] **Phase 20: Port Definition & Contracts** — NOTIF-01..04 — 1/1 plan complete — 2026-05-08
- [ ] **Phase 21: Serilog Notification Adapter** — SERILOG-01..04 — zero-breakage bridge to existing 13 channels
- [ ] **Phase 22: Telegram HTTP Adapter** — TELEGRAM-01..04 — proves extensibility
- [ ] **Phase 23: DI Wiring & AppService Migration** — WIRING-01..05 — wire everything together

</details>

<details>
<summary>✅ v4.0.0.7 Bili Account Management (Phases 17–19) — SHIPPED 2026-05-07</summary>

- [x] **Phase 17: Account Storage Foundation** — 1/1 plan complete — 2026-05-06
- [x] **Phase 18: Account CRUD Operations** — 2/2 plans complete — 2026-05-07
- [x] **Phase 19: QR Code Login** — 1/1 plan complete — 2026-05-07

</details>

<details>
<summary>✅ v4.0.0.6 Web Layer Boundary Cleanup (Phases 13–16) — SHIPPED 2026-05-05</summary>

- [x] **Phase 13: Web Boundary Foundation** — 2/2 plans complete — 2026-05-05
- [x] **Phase 14: Auth And Admin UI Boundary Cleanup** — 2/2 plans complete — 2026-05-05
- [x] **Phase 15: Scheduler UI Boundary Cleanup** — 3/3 plans complete — 2026-05-05
- [x] **Phase 16: Web Composition And Regression Verification** — 2/2 plans complete — 2026-05-05

</details>

<details>
<summary>✅ v4.0.0.1 Brownfield Refactor (Phases 1–6) — SHIPPED 2026-05-03</summary>

- [x] **Phase 1: Boundary Guardrails** — 2/2 plans complete — 2026-05-02
- [x] **Phase 2: Host Safety Nets** — 3/3 plans complete — 2026-05-03
- [x] **Phase 3: Login Refactor Slice** — 1/1 plan complete — 2026-05-03
- [x] **Phase 4: DailyTask Refactor Slice** — 1/1 plan complete — 2026-05-03
- [x] **Phase 5: Scheduler Shell Cleanup** — 2/2 plans complete — 2026-05-03
- [x] **Phase 6: Integration Boundary And Failure Model** — 4/4 plans complete — 2026-05-03

</details>

<details>
<summary>✅ v4.0.0.2 AppService Refactor Continuation (Phase 7) — SHIPPED 2026-05-04</summary>

- [x] **Phase 7: AppService Cookie Handling Extraction** — 4/4 plans complete — 2026-05-04

</details>

<details>
<summary>✅ v4.0.0.3 Refit Migration (Phases 8–10) — SHIPPED 2026-05-04</summary>

- [x] **Phase 8: Refit Foundation** — 1/1 plan complete — 2026-05-04
- [x] **Phase 9: Bilibili Interface Migration** — 2/2 plans complete — 2026-05-04
- [x] **Phase 10: DI Migration & Cleanup** — 1/1 plan complete — 2026-05-04

</details>

<details>
<summary>✅ v4.0.0.4 Agent Interface Consolidation (Phase 11) — SHIPPED 2026-05-04</summary>

- [x] **Phase 11: Agent Interface Consolidation** — 4/4 plans complete — 2026-05-04

</details>

<details>
<summary>✅ v4.0.0.5 Agent DTO Reorganization (Phase 12) — SHIPPED 2026-05-04</summary>

- [x] **Phase 12: Agent DTO Reorganization** — 2/2 plans complete — 2026-05-04

</details>

## Phase Details — v4.0.0.8

### Phase 20: Port Definition & Contracts
**Goal**: Developers can depend on a single `INotificationService` port in Application.Contracts, with strongly-typed DTOs for messages and summaries, and an ArchUnit guardrail preventing Infrastructure leakage
**Depends on**: Nothing (first phase of milestone)
**Requirements**: NOTIF-01, NOTIF-02, NOTIF-03, NOTIF-04
**Success Criteria**:
  1. Application-layer code can reference `INotificationService` from `Application.Contracts` with `SendAsync` and `SendSummaryAsync` methods
  2. `NotificationMessage` value object carries Title, Body, NotificationLevel (Info/Warning/Error), and optional GroupKey
  3. `SummaryLine` value object carries Label, Value, and StatusIcon for structured task-result summaries
  4. The port interface is async-only (all methods return `Task`) and does not reference Serilog, ILogger, or any Infrastructure type
  5. An ArchUnitNET test enforces that Application-layer code may reference `INotificationService` from `Application.Contracts` but may not directly reference any `Infrastructure.Notifications` adapter class
**Plans**: 1 plan

Plans:
- [x] 20-01-PLAN.md — Port definition, DTOs, and ArchUnit guardrail — complete ✓

### Phase 21: Serilog Notification Adapter
**Goal**: A `SerilogNotificationAdapter` implements `INotificationService` by routing notification content through the existing Serilog pipeline, so all 13 sink channels keep working identically with zero breaking changes
**Depends on**: Phase 20
**Requirements**: SERILOG-01, SERILOG-02, SERILOG-03, SERILOG-04
**Success Criteria**:
  1. `SerilogNotificationAdapter` registered as default `INotificationService` implementation writes notification content through `ILogger` into the existing Serilog pipeline
  2. All 13 existing Serilog sink channels (Console, Debug, File, Telegram, WorkWeChat×2, DingTalk, ServerChan, CoolPush, OtherApi, PushPlus, Teams, Gotify) continue working identically after the adapter is registered
  3. The adapter preserves `BatchSinkManager.FlushAsync(fireInstanceId)` batch-at-end timing by tagging log entries with `GroupPropertyKey` so sinks batch and flush at job end
  4. The adapter uses `Log.ForContext("NotificationOrigin", "adapter")` so adapter error logs do not re-enter the notification sink pipeline (prevents circular dependency)
**Plans**: TBD

### Phase 22: Telegram HTTP Adapter
**Goal**: A native `TelegramHttpAdapter` implements `INotificationService` by sending notifications as HTTP POST requests to the Telegram Bot API, proving the port/adapter boundary enables non-Serilog extensibility
**Depends on**: Phase 20
**Requirements**: TELEGRAM-01, TELEGRAM-02, TELEGRAM-03, TELEGRAM-04
**Success Criteria**:
  1. `TelegramHttpAdapter` sends notifications as native HTTP POST requests to `https://api.telegram.org/bot{token}/sendMessage`
  2. The adapter reads configuration from `Notification:Telegram` section via `IOptionsMonitor<TelegramNotificationOptions>`, supporting `botToken`, `chatId`, `apiHost`, and `proxy`
  3. The adapter handles HTTP 429 (rate limit) responses by respecting the `retry_after` value before retrying
  4. The adapter truncates message bodies exceeding 4096 characters and appends a `…[truncated]` indicator
**Plans**: TBD

### Phase 23: DI Wiring & AppService Migration
**Goal**: `INotificationService` is wired into both Console and Web hosts with configuration-driven adapter selection, and key AppServices route task-completion summaries through the port
**Depends on**: Phase 21, Phase 22
**Requirements**: WIRING-01, WIRING-02, WIRING-03, WIRING-04, WIRING-05
**Success Criteria**:
  1. `INotificationService` is registered in both Console and Web host DI containers; when `Notification:Telegram` config is absent, `SerilogNotificationAdapter` is default; when present, a `CompositeNotificationAdapter` fans out to both Serilog and Telegram adapters
  2. `DailyTaskAppService` and `LoginTaskAppService` send a structured notification summary through `INotificationService.SendSummaryAsync()` at task completion, in addition to existing `ILogger` diagnostic logging
  3. The remaining 9 AppServices route task-completion notifications through `INotificationService`
  4. An ArchUnitNET test enforces that Application-layer code may reference `INotificationService` from `Application.Contracts` but may not directly reference any `Infrastructure.Notifications` adapter class
  5. Build 0 errors; ArchitectureTests pass; IntegrationTests pass; existing behavior preserved
**Plans**: TBD

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 20. Port Definition & Contracts | 1/1 | ✓ Complete | 2026-05-08 |
| 21. Serilog Notification Adapter | 0/1 | Not started | — |
| 22. Telegram HTTP Adapter | 0/1 | Not started | — |
| 23. DI Wiring & AppService Migration | 0/1 | Not started | — |

All milestones complete through v4.0.0.7. Full phase/plan history archived in milestone directories.
