# Phase 20: Port Definition & Contracts - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-07
**Phase:** 20-Port-Definition-And-Contracts
**Areas discussed:** Interface method shape, GroupKey threading model, NotificationMessage & SummaryLine relationship

---

## Interface Method Shape

| Option | Description | Selected |
|--------|-------------|----------|
| Single `SendAsync` | One method. Adapter decides batching vs immediate. Simplest port contract. | |
| Dual: `SendAsync` + `SendSummaryAsync` | Separate methods for per-event and job-end digests. Explicit calling intent. | ✓ |
| Triple: add `FlushAsync` | Three methods including FlushAsync. Most expressive but flush belongs to batching layer. | |

**User's choice:** Initially selected "Triple: add `FlushAsync`" but then clarified: "dont know, if it is actually no need, just dont add it"
**Notes:** After discussion, user deferred to builder judgment. Decision: Dual methods. FlushAsync excluded because flushing is BatchSinkManager's concern, not the port's. Serilog adapter writes to ILogger and batching happens automatically. Telegram adapter sends immediately — no batching needed.

---

## GroupKey Threading Model

| Option | Description | Selected |
|--------|-------------|----------|
| Explicit parameter | Each SendAsync/SendSummaryAsync call includes groupKey. More testable, no ambient state coupling. | ✓ |
| Ambient LogContext | Read from Serilog's LogContext like BaseJob. Simpler for callers but couples port to Serilog. | |
| Scoped property | Property set once per job. Testable, no per-call repetition. | |

**User's choice:** Explicit parameter (recommended)
**Notes:** Decouples the port from Serilog's LogContext. Callers pass fireInstanceId explicitly. Serilog adapter may internally push to LogContext, but that's adapter-internal.

---

## NotificationMessage & SummaryLine Relationship

| Option | Description | Selected |
|--------|-------------|----------|
| Separate types | Two unrelated records. SendAsync takes NotificationMessage; SendSummaryAsync takes SummaryLine[]. Each has its own shape. | ✓ |
| Common base + subtype | One base type, SummaryLine as subtype. Single inheritance tree but forces artificial commonality. | |
| Single type with optional summary | NotificationMessage contains optional SummaryLines[] field. One type handles both. | |

**User's choice:** Separate types (recommended)
**Notes:** Different delivery semantics — event messages vs. summary digests. No inheritance relationship.

---

## Agent's Discretion

- Whether `NotificationMessage.GroupKey` field is used (method param is authoritative; DTO field for serialization convenience)
- Whether `SummaryLine.StatusIcon` should be named `StatusIcon` or `Status`
- Exact namespace structure under `Application.Contracts` (suggested: `Ray.BiliBiliTool.Application.Contracts.Notifications`)
- Whether to add XML doc comments on interface methods and DTOs
- Whether to use `IReadOnlyList<SummaryLine>` vs `SummaryLine[]` in `SendSummaryAsync`

## Deferred Ideas

- NotificationLevel enum extension (Critical, Success) — 3-level sufficient for now
- IMessageFormatter abstraction for message rendering — each adapter handles its own
- Rate limiting at port level — adapter-specific concern
- Notification history/audit log — future milestone
