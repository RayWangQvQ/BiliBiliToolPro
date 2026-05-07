# Requirements — v4.0.0.8 Notification Boundary

**Milestone:** v4.0.0.8 Notification Boundary
**Created:** 2026-05-07
**Status:** Draft

## Milestone Goal

Extract the notification concern behind an explicit `INotificationService` port/adapter boundary, replacing the current direct Serilog-sink-only approach, and prove the boundary works by adding a native Telegram HTTP adapter alongside the existing 13 channels.

---

## Requirements

### Category: Port & Contracts (NOTIF)

- [ ] **NOTIF-01**: Maintainer can depend on a single `INotificationService` interface in `Application.Contracts` with `SendAsync(NotificationMessage)` and `SendSummaryAsync(IEnumerable<SummaryLine>, string title)` methods
- [ ] **NOTIF-02**: A `NotificationMessage` value object carries `Title`, `Body`, `NotificationLevel` (Info/Warning/Error), and an optional `GroupKey` for batch-at-end correlation
- [ ] **NOTIF-03**: A `SummaryLine` value object carries `Label`, `Value`, and `StatusIcon` for structured task-result summaries
- [ ] **NOTIF-04**: The port interface is async-only (all methods return `Task`) and does not reference Serilog, `ILogger`, or any Infrastructure type

### Category: Serilog Adapter (SERILOG)

- [ ] **SERILOG-01**: A `SerilogNotificationAdapter` implements `INotificationService` by writing notification content through `ILogger` into the existing Serilog pipeline
- [ ] **SERILOG-02**: All 13 existing Serilog sink channels (Console, Debug, File, Telegram, WorkWeChat×2, DingTalk, ServerChan, CoolPush, OtherApi, PushPlus, Teams, Gotify) continue working identically after the adapter is registered
- [ ] **SERILOG-03**: The Serilog adapter preserves `BatchSinkManager.FlushAsync(fireInstanceId)` batch-at-end timing by tagging log entries with `GroupPropertyKey` so sinks batch and flush at job end
- [ ] **SERILOG-04**: The Serilog adapter uses `Log.ForContext("NotificationOrigin", "adapter")` so that adapter error logs do not re-enter the notification sink pipeline (prevents circular dependency)

### Category: Telegram HTTP Adapter (TELEGRAM)

- [ ] **TELEGRAM-01**: A `TelegramHttpAdapter` implements `INotificationService` by sending notifications as native HTTP POST requests to the Telegram Bot API (`https://api.telegram.org/bot{token}/sendMessage`)
- [ ] **TELEGRAM-02**: The Telegram adapter reads configuration from `Notification:Telegram` section via `IOptionsMonitor<TelegramNotificationOptions>`, supporting `botToken`, `chatId`, `apiHost` (custom API base URL), and `proxy` (HTTP proxy in `user:password@host:port` format)
- [ ] **TELEGRAM-03**: The Telegram adapter handles HTTP 429 (rate limit) responses by respecting the `retry_after` value from the response body before retrying
- [ ] **TELEGRAM-04**: The Telegram adapter truncates message bodies exceeding 4096 characters and appends a `…[truncated]` indicator

### Category: DI Wiring & AppService Migration (WIRING)

- [ ] **WIRING-01**: `INotificationService` is registered in both Console and Web host DI containers; when `Notification:Telegram` config section is absent, the `SerilogNotificationAdapter` is the default; when present, a `CompositeNotificationAdapter` fans out to both Serilog and Telegram adapters
- [ ] **WIRING-02**: `DailyTaskAppService` and `LoginTaskAppService` send a structured notification summary through `INotificationService.SendSummaryAsync()` at task completion, in addition to existing `ILogger` diagnostic logging
- [ ] **WIRING-03**: The remaining 9 AppServices (LiveTaskAppService, MangaTaskAppService, VipBigPointAppService, etc.) route task-completion notifications through `INotificationService`
- [ ] **WIRING-04**: An ArchUnitNET test enforces that Application-layer code may reference `INotificationService` from `Application.Contracts` but may not directly reference any `Infrastructure.Notifications` adapter class
- [ ] **WIRING-05**: Build 0 errors; ArchitectureTests pass; IntegrationTests pass; existing behavior preserved (characterization tests unchanged)

---

## Future Requirements (deferred)

- [ ] TEST-04: Maintainer can verify key Web or Blazor components with dedicated component tests
- [ ] TEST-05: Maintainer can enforce focused coverage thresholds for critical modules in CI
- [ ] FLOW-05: Maintainer can unify Console and Web configuration and startup composition paths where behavior meaningfully overlaps
- [ ] QUAL-03: Maintainer can remove default credential risks and similar obvious safety issues from bootstrap flows
- [ ] QUAL-04: Maintainer can reduce repository noise from generated outputs so searches and reviews focus on source of truth files
- [ ] NOTIF-F1: Maintainer can configure conditional channel routing (e.g., "errors only to Telegram, all to DingTalk")
- [ ] NOTIF-F2: Maintainer can use notification message templates for structured task-result formatting
- [ ] NOTIF-F3: Maintainer can configure per-channel format overrides (Markdown for Telegram, plain text for others)

## Out of Scope

- **Real-time / push notifications** — Current architecture batches at job end; changing to real-time would break the `BatchSinkManager.FlushAsync()` contract
- **Custom message queue / event bus** — Over-engineered for a scheduled task automation tool; direct `await notificationService.SendAsync()` is sufficient
- **Notification history / persistence** — Storing sent notifications in DB adds schema, queries, and UI; pure scope creep
- **User-facing notification configuration UI** — Configuration stays in `appsettings.json` matching all existing Serilog sink config patterns
- **Webhook receiver / bidirectional notifications** — Completely different concern (bot framework)
- **Notification templating engine** — Raw `ILogger` messages become the notification body; templating is a future concern
- **MarkdownV2 escaping for Telegram** — Free once adapter is built but adds scope; default to plain text for v1

---

## Traceability

_Roadmap created 2026-05-07. Phases 20–23._

| Requirement | Phase | Status |
|-------------|-------|--------|
| NOTIF-01 | Phase 20 | Pending |
| NOTIF-02 | Phase 20 | Pending |
| NOTIF-03 | Phase 20 | Pending |
| NOTIF-04 | Phase 20 | Pending |
| SERILOG-01 | Phase 21 | Pending |
| SERILOG-02 | Phase 21 | Pending |
| SERILOG-03 | Phase 21 | Pending |
| SERILOG-04 | Phase 21 | Pending |
| TELEGRAM-01 | Phase 22 | Pending |
| TELEGRAM-02 | Phase 22 | Pending |
| TELEGRAM-03 | Phase 22 | Pending |
| TELEGRAM-04 | Phase 22 | Pending |
| WIRING-01 | Phase 23 | Pending |
| WIRING-02 | Phase 23 | Pending |
| WIRING-03 | Phase 23 | Pending |
| WIRING-04 | Phase 23 | Pending |
| WIRING-05 | Phase 23 | Pending |
