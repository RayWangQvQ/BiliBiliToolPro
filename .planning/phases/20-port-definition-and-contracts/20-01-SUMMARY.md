---
phase: 20-port-definition-and-contracts
plan: 01
subsystem: notifications
tags: [notification, port-adapter, archunit, contracts]

requires:
  - phase: none
    provides: N/A — first phase of milestone
provides:
  - INotificationService port interface in Application.Contracts with SendAsync and SendSummaryAsync
  - NotificationMessage, SummaryLine, NotificationLevel value types
  - ArchUnit guardrail preventing Infrastructure.Notification leakage into Application layer
affects:
  - Phase 21 (Serilog Notification Adapter) — implements this port
  - Phase 22 (Telegram HTTP Adapter) — implements this port
  - Phase 23 (DI Wiring & AppService Migration) — wires this port

tech-stack:
  added: []
  patterns:
    - "Port interface in Application.Contracts with async-only methods and groupKey parameter"
    - "Value types as C# records (immutable, stub-friendly for tests)"
    - "NotificationLevel enum decoupled from Serilog's LogEventLevel"

key-files:
  created:
    - src/Ray.BiliBiliTool.Application.Contracts/Notifications/INotificationService.cs
    - src/Ray.BiliBiliTool.Application.Contracts/Notifications/NotificationMessage.cs
    - src/Ray.BiliBiliTool.Application.Contracts/Notifications/SummaryLine.cs
    - src/Ray.BiliBiliTool.Application.Contracts/Notifications/NotificationLevel.cs
  modified:
    - test/Ray.BiliBiliTool.ArchitectureTests/DependencyGuardrailTests.cs

key-decisions:
  - "INotificationService uses two async methods (SendAsync + SendSummaryAsync) with explicit groupKey parameter — no FlushAsync, no ambient LogContext dependency (D-01, D-02, D-03)"
  - "NotificationLevel enum is domain-specific (Info/Warning/Error), intentionally NOT mirroring Serilog's LogEventLevel (D-06)"
  - "NotificationMessage and SummaryLine are completely separate record types with no inheritance — different delivery semantics (D-07)"
  - "ArchUnit test passes vacuously now (Infrastructure.Notifications namespace is empty) and becomes a live guardrail when adapters land in Phase 21 (D-08)"

patterns-established:
  - "Port/adapter pattern for notification concern: interface in Application.Contracts, implementations in Infrastructure"

requirements-completed:
  - NOTIF-01
  - NOTIF-02
  - NOTIF-03
  - NOTIF-04

duration: 5min
completed: 2026-05-08
---

# Phase 20: Port Definition & Contracts Summary

**INotificationService port with SendAsync/SendSummaryAsync contracts, NotificationMessage/SummaryLine value types, and ArchUnit guardrail — foundation for the notification boundary milestone.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-05-08
- **Completed:** 2026-05-08
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Created `INotificationService` port interface in `Application.Contracts.Notifications` with two async methods accepting explicit `groupKey` parameter (no FlushAsync, no ambient LogContext)
- Created `NotificationMessage` record (Title, Body, NotificationLevel, optional GroupKey), `SummaryLine` record (Label, Value, optional StatusIcon), and `NotificationLevel` enum (Info/Warning/Error)
- Added ArchUnit guardrail test `Application_should_not_depend_on_notification_adapters` — enforces Application layer cannot reference Infrastructure.Notification types (6/6 architecture tests pass)
- All acceptance criteria verified: 0 build errors, 7/7 integration tests pass, 6/6 architecture tests pass

## Task Commits

Each task was committed atomically:

1. **Task 1: Create INotificationService port and DTOs** + **Task 2: Add ArchUnit notification layering guardrail** - `c6ec918` (feat)

**Plan metadata:** committed with task

## Files Created/Modified

- `src/Ray.BiliBiliTool.Application.Contracts/Notifications/INotificationService.cs` — Port interface with `SendAsync` and `SendSummaryAsync`
- `src/Ray.BiliBiliTool.Application.Contracts/Notifications/NotificationMessage.cs` — Notification message value object (record)
- `src/Ray.BiliBiliTool.Application.Contracts/Notifications/SummaryLine.cs` — Summary line value object (record)
- `src/Ray.BiliBiliTool.Application.Contracts/Notifications/NotificationLevel.cs` — Notification severity enum (Info/Warning/Error)
- `test/Ray.BiliBiliTool.ArchitectureTests/DependencyGuardrailTests.cs` — Added `NotificationAdapters` provider and `Application_should_not_depend_on_notification_adapters` test

## Decisions Made

- `groupKey` is an explicit method parameter (not ambient LogContext) — decouples port from Serilog internals (D-03)
- `FlushAsync` deliberately excluded from the port — flushing is `BatchSinkManager`'s infrastructure concern (D-02)
- `NotificationLevel` is a domain-specific enum (Info/Warning/Error), NOT a mirror of Serilog's `LogEventLevel` (D-06)
- `NotificationMessage` and `SummaryLine` are completely separate types — event messages vs. summary digests have different delivery semantics (D-07)
- ArchUnit test passes vacuously now (no types in `Infrastructure.Notifications` namespace yet) — becomes a live guardrail when Phase 21 creates the Serilog adapter (D-08)

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- [x] Build 0 errors (81 pre-existing warnings)
- [x] ArchitectureTests 6/6 pass (5 existing + 1 new)
- [x] IntegrationTests 7/7 pass
- [x] All 4 contract files exist in `Application.Contracts/Notifications/`
- [x] No `using Serilog` or `ILogger` type references in contract files (Serilog mentioned only in XML doc comments)
- [x] No `FlushAsync` in the port interface
- [x] Git commit `c6ec918` contains all 5 changed files
