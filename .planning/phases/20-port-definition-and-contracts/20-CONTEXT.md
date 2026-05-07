# Phase 20: Port Definition & Contracts - Context

**Gathered:** 2026-05-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Define the `INotificationService` port interface and its supporting DTOs (`NotificationMessage`, `SummaryLine`, `NotificationLevel`) in `Application.Contracts`. Add an ArchUnit guardrail test to enforce layering — Application-layer code may reference `INotificationService` but may not directly reference any Infrastructure adapter class.

Phase 20 delivers only the contract (interface + value types + test). No adapters, no DI wiring, no migration of existing callers. Phase 21 (Serilog adapter) and Phase 22 (Telegram adapter) will implement the port; Phase 23 will wire everything together.

</domain>

<decisions>
## Implementation Decisions

### A: Interface Method Shape (D-01, D-02)

- **D-01:** `INotificationService` exposes **two async methods**: `Task SendAsync(NotificationMessage message, string groupKey)` and `Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)`. These are the only entry points on the port.
- **D-02:** `FlushAsync` is deliberately excluded from the port interface. Flushing is a batching infrastructure concern (owned by `BatchSinkManager`), not a notification delivery concern. The Serilog adapter writes via `ILogger` and lets `BatchSinkManager` handle batching automatically. The Telegram adapter sends immediately on `SendAsync`. Callers never need to flush the notification port explicitly.

### B: GroupKey Threading Model (D-03)

- **D-03:** Both methods accept `string groupKey` as an **explicit parameter**. This decouples the port from Serilog's `LogContext` ambient state. Callers (AppServices) pass `fireInstanceId` (obtained from the job context or propagated down from `BaseJob`). The Serilog adapter may internally push the groupKey into LogContext for downstream sink routing, but that's an implementation detail of the adapter, not a contract obligation.

### C: DTO Model — Separate Types (D-04, D-05, D-06, D-07)

- **D-04:** `NotificationMessage` is a record: `record NotificationMessage(string Title, string Body, NotificationLevel Level, string? GroupKey = null)`. It carries the notification content and severity. The `GroupKey` field on the DTO is optional and for convenience; the method parameter takes precedence.
- **D-05:** `SummaryLine` is a record: `record SummaryLine(string Label, string Value, string? StatusIcon = null)`. It represents one line in a task-end summary digest (e.g., "投币" → "5 个" with status "✅"). `StatusIcon` is optional text/emoji for structured display.
- **D-06:** `NotificationLevel` is a domain-specific enum: `enum NotificationLevel { Info, Warning, Error }`. It intentionally does NOT mirror Serilog's `LogEventLevel` — the port is Serilog-agnostic. The Serilog adapter maps `Info → Information`, `Warning → Warning`, `Error → Error`.
- **D-07:** `NotificationMessage` and `SummaryLine` are completely separate types with no inheritance relationship. They serve different delivery semantics: event messages vs. summary digests. Forcing a common base would create artificial coupling.

### D: ArchUnit Guardrail (D-08)

- **D-08:** An ArchUnitNET test enforces: Application-layer projects (`Application`) may reference `INotificationService` from `Application.Contracts`, but may NOT reference any class in `Infrastructure.Notifications` namespace. This is the same pattern as the existing 5/5 ArchUnit layer tests (Phase 1). The test uses `Types().That().ResideInNamespace("Ray.BiliBiliTool.Infrastructure.Notifications")` and verifies no Application-layer type depends on them.

### Agent's Discretion

- Whether `NotificationMessage.GroupKey` field is used at all (the method parameter is authoritative; the DTO field exists for serialization convenience)
- Whether `SummaryLine.StatusIcon` should be named `StatusIcon` or `Status` (string, not an enum — icons are freeform text/emoji)
- Exact namespace structure under `Application.Contracts` (suggested: `Ray.BiliBiliTool.Application.Contracts.Notifications`)
- Whether to add XML doc comments on the interface methods and DTOs
- Whether to add a `IReadOnlyList<SummaryLine>` instead of `SummaryLine[]` in the `SendSummaryAsync` signature

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing Port/Adapter Patterns
- `src/Ray.BiliBiliTool.Application.Contracts/IAppService.cs` — existing base port interface pattern; `INotificationService` lives alongside it
- `src/Ray.BiliBiliTool.Application.Contracts/IDailyTaskAppService.cs` — example concrete interface with `[Description]` attribute

### Notification Pipeline (current implementation)
- `src/Ray.BiliBiliTool.Web/Jobs/BaseJob.cs` — `GroupPropertyKey` push + `BatchSinkManager.FlushAsync(fireInstanceId)` pattern; the code that the Serilog adapter must integrate with
- `Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey` — external NuGet constant for LogContext property name

### ArchUnit Enforcement (existing guardrails)
- `test/Ray.BiliBiliTool.ArchitectureTests/` — existing ArchUnitNET tests enforcing layer direction; new notification guardrail follows same pattern

### Milestone Research
- `.planning/research/ARCHITECTURE.md` — port/adapter architecture with suggested DTO shapes and namespace placement
- `.planning/research/PITFALLS.md` — 8 critical pitfalls including batch flush breakage, dual-write/duplicate messages, circular dependency
- `.planning/research/FEATURES.md` — Telegram Bot API specifics (4096 char limit, rate limits, proxy support) relevant to adapter implementations
- `.planning/research/STACK.md` — zero new runtime NuGet packages; Telegram via HttpClient

### Requirements & Roadmap
- `.planning/REQUIREMENTS.md` — NOTIF-01 (interface), NOTIF-02 (DTOs), NOTIF-03 (async-only, no Serilog refs), NOTIF-04 (ArchUnit guardrail)
- `.planning/ROADMAP.md` — Phase 20 goal and success criteria

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IAppService` / `Application.Contracts` project — established namespace and project structure for ports
- `BaseJob.cs` LogContext/GroupPropertyKey pattern — shows how groupKey flows through the system; callers get fireInstanceId from job context
- Existing ArchUnit tests — pattern for adding a new layering rule

### Established Patterns
- Port interfaces in `Application.Contracts` (never in a layer project)
- Async-only contracts (all `IAppService` methods return `Task`)
- Value types as records (immutable, trivially stub in tests)
- Layer direction tests via ArchUnitNET (pattern from Phase 1)

### Integration Points
- `Application.Contracts.csproj` — add new `Notifications/` folder with interface and DTOs
- `test/Ray.BiliBiliTool.ArchitectureTests/` — add new ArchUnit test for notification layering
- Phase 21 (Serilog adapter) will reference `INotificationService` from Application.Contracts and implement it in Infrastructure
- Phase 22 (Telegram adapter) will do the same — two implementations of the same port

</code_context>

<specifics>
## Specific Ideas

- The `sendSummaryAsync` method takes `string title` + `SummaryLine[]` rather than a separate `SummaryMessage` type. The title is the job name (e.g., "Daily"), and lines are the individual results. Keeping them as separate parameters avoids an extra type that's only used in one place.
- Phase 21's Serilog adapter will call `ILogger.LogInformation()` which enters the existing Serilog pipeline and gets batched by `BatchSinkManager` automatically. The adapter does NOT call `BatchSinkManager` directly.
- Phase 22's Telegram adapter will call the Telegram Bot API HTTP endpoint directly. It bypasses Serilog entirely — that's the whole point of the port/adapter boundary.
- Config-driven auto-disable: when native Telegram adapter is registered, the Serilog TelegramBatched sink should be auto-disabled to prevent duplicate messages. This is a Phase 23 concern, not Phase 20.

</specifics>

<deferred>
## Deferred Ideas

- NotificationLevel enum extension (e.g., `Critical`, `Success`) — deferred; 3-level enum is sufficient for now
- IMessageFormatter abstraction for message rendering — deferred; each adapter handles its own formatting
- Rate limiting at the port level — deferred; rate limiting is adapter-specific (Telegram: 1 msg/sec per chat)
- Notification history/audit log — deferred to a future milestone

</deferred>

---

*Phase: 20-Port-Definition-And-Contracts*
*Context gathered: 2026-05-07*
