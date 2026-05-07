# BiliBiliToolPro Refactor And Optimization

## What This Is

This project is a brownfield refactor of the existing BiliBiliToolPro codebase. The goal is not to replace the product, but to make the current system easier to change by clarifying layer boundaries, reducing coupling between modules, and adding enough test coverage to support safe incremental improvements.

The product being preserved is an automated Bilibili task execution system with Console, Web, scheduling, API-integration, and deployment surfaces. The work now is to restructure that existing system so maintainers can evolve it with lower regression risk.

## Core Value

Make the existing codebase safe to change: clear boundaries, lower coupling, and testable critical flows.

## Current Milestone: v4.0.0.8 Notification Boundary

**Goal:** Extract the notification concern behind an explicit `INotificationService` port/adapter boundary, replacing the current direct Serilog-sink-only approach, and prove the boundary works by adding a native Telegram HTTP adapter alongside the existing 13 channels.

**Target features:**
- `INotificationService` port — Application-layer interface for "send task results/summaries to the user", separated from diagnostic logging
- Serilog adapter — Default adapter that routes notification calls through the existing 13 Serilog sinks (zero breaking change)
- Telegram HTTP adapter — New native HTTP-based Telegram channel behind the port, proving extensibility without Serilog dependency
- Migration — Task completion, error summaries, and other "notification-worthy" signals routed through the new port; regular debug logging stays on `ILogger`
- Configuration — All existing Serilog sink config in `appsettings.json` continues to work; new Telegram adapter has its own config section

## Shipped: v4.0.0.7 Bili Account Management (SHIPPED 2026-05-07)

**Goal:** Add a Web-based "Bili Account" page for viewing, adding, editing, and deleting Bili accounts (cookies), backed by SQLite as the sole configuration source for Web, replacing `cookies.json`.

**Delivered:**
- Account list view with MudTable (UserId + cookie string)
- Full CRUD: add (paste cookie), edit, delete (with re-keying), reorder (atomic swap)
- QR code login in browser — QrLoginDialog with state machine, polling, retry
- `cookies.json` retained as fallback (SQLite wins for overlapping keys)
- `IBiliAccountPageWorkflow` seam (8 methods) following v4.0.0.6 pattern
- 7/7 ACCT requirements satisfied | Build 0 errors | Arch 5/5 | Integration 7/7 | Components 28/28

## Shipped: v4.0.0.6 Web Layer Boundary Cleanup

**Goal:** Refactor the Web layer so UI code stays thin, business logic is isolated behind clearer boundaries, and the resulting flow is safer to evolve and test.

**Target features:**
- Separate UI rendering concerns from business orchestration in the Web layer
- Reduce page and component ownership of workflow logic and move orchestration behind clearer application boundaries
- Add focused validation coverage around the refactored Web or Blazor slices so behavior remains stable during cleanup

## Requirements

### Validated

- ✓ The system can execute Bilibili automation tasks through existing Console and Web hosts — existing
- ✓ The system already integrates with Bilibili APIs, scheduling, persistence, and multiple deployment targets — existing
- ✓ The system already supports operational workflows such as login, daily tasks, manga/live tasks, notifications, and multi-environment deployment — existing
- ✓ ARCH-01: Maintainer can enforce explicit dependency direction between all 5 layers — v4.0.0.1
- ✓ ARCH-02: Web and Console hosts stay thin; startup code no longer owns business orchestration — v4.0.0.1
- ✓ ARCH-03: Core modules compose through module-level registration entry points — v4.0.0.1
- ⚠ ARCH-04 (partial): EF + HTTP Agent boundaries established; notification boundary → v4.0.0.8 — v4.0.0.1
- ✓ TEST-01: Login and DailyTask observable behavior frozen with characterization tests — v4.0.0.1
- ✓ TEST-02: Startup, config, EF, HTTP, and scheduling validated via host integration tests — v4.0.0.1
- ✓ TEST-03: Cross-layer dependency violations detected through ArchUnitNET architecture tests — v4.0.0.1
- ✓ FLOW-01: Login flow refactored behind a clearer application boundary — v4.0.0.1
- ✓ FLOW-02: DailyTask flow refactored behind a testable application boundary — v4.0.0.1
- ✓ FLOW-03: All 12 Quartz job classes reduced to thin delegation shells — v4.0.0.1
- ✓ FLOW-04: Bilibili HTTP integrations normalized through consistent Agent boundary with explicit policies — v4.0.0.1
- ✓ QUAL-01: BiliException typed hierarchy (Business/Integration/Validation) with 14 DomainService conversions — v4.0.0.1
- ✓ QUAL-02: TaskFlowDiagnosticScope diagnostic markers for Login and DailyTask comparison — v4.0.0.1

- ✓ FLOW-06: Shared SetCookie/SaveCookie behavior defined exactly once in `BaseMultiAccountsAppService` (protected virtual), not copied across 11 AppServices — v4.0.0.2
- ✓ FLOW-07: Refactored AppService hierarchy preserves all observable behavior — ArchitectureTests 4/4, IntegrationTests 7/7, UAT 7/7 — v4.0.0.2
- ✓ QUAL-02: TaskFlowDiagnosticScope diagnostic markers extended to all 11 in-scope AppServices — v4.0.0.2

- ✓ REFIT-01: All 17 Bilibili HTTP client interfaces use Refit attributes — v4.0.0.3
- ✓ REFIT-02: IQingLongApi uses Refit attributes — v4.0.0.3
- ✓ REFIT-03: DI registration uses AddRefitClient<T> with same handlers and Polly policies — v4.0.0.3
- ✓ REFIT-04: IBiliBiliApi common headers handled by BiliBiliCommonHeadersDelegatingHandler — v4.0.0.3
- ✓ REFIT-05: WebApiClientCore package removed; 4 legacy attribute files deleted — v4.0.0.3
- ✓ REFIT-06: Build 0 errors; architecture tests 4/4; integration tests 7/7 — v4.0.0.3

- ✓ DTO-01: Agent-layer DTO folders mirror interface boundaries — v4.0.0.5
- ✓ DTO-02: `IApiApi`-owned DTOs live under `Dtos/ApiApi/...`, including `Dtos/ApiApi/UpInfo/` with namespace `Dtos.ApiApi.UpInfo` — v4.0.0.5
- ✓ DTO-03: `CoinBalance.cs` moved to `Dtos/AccountApi/` (namespace updated) — v4.0.0.5
- ✓ DTO-04: Nav DTO group lives under `Dtos/NavApi/`; live DTO ownership split between `LiveApi` and `LiveTraceApi` — v4.0.0.5
- ✓ DTO-05: All consumers updated with corrected using directives — v4.0.0.5
- ✓ DTO-06: Build 0 errors | Arch 4/4 | Integration 7/7 — v4.0.0.5

- ✓ WEB-01: Targeted Web pages and dialogs use explicit Web-facing coordination seams (`ILoginPageStateFactory`, `IAdminPageWorkflow`, `ISchedulerPageWorkflow`, `ILogsDialogWorkflow`, `IHistoryDialogWorkflow`) — v4.0.0.6
- ✓ WEB-02: Scheduler pages and dialogs no longer own repository-backed log polling, execution-history loading, or scheduler-event orchestration directly — v4.0.0.6
- ✓ WEB-03: Login and admin page validation, success-flow handling, and navigation orchestration are isolated from targeted component markup logic — v4.0.0.6
- ✓ WEB-04: Web host composition and Web registrations remain wiring-focused; ArchUnit guardrail enforces seam usage — v4.0.0.6
- ✓ WEB-05: `Ray.BiliBiliTool.Web.ComponentTests` project with 28 bUnit tests covers refactored Web slices — v4.0.0.6
- ✓ WEB-06: Build 0 errors | Arch 5/5 | Integration 7/7 | Components 28/28 — v4.0.0.6

- ✓ ACCT-01: Maintainer can view a list of all Bili accounts (UserId + cookie string) in the Web UI — v4.0.0.7
- ✓ ACCT-02: Maintainer can add a new Bili account by scanning a QR code in the Web browser — v4.0.0.7
- ✓ ACCT-03: Maintainer can add a new Bili account by pasting a raw cookie string — v4.0.0.7
- ✓ ACCT-04: Maintainer can edit an existing account's cookie string — v4.0.0.7
- ✓ ACCT-05: Maintainer can delete an existing account — v4.0.0.7
- ✓ ACCT-06: Maintainer can reorder accounts to change execution order — v4.0.0.7
- ✓ ACCT-07: Web host keeps cookies.json as fallback; SQLite bili_appsettings is highest-priority config source — v4.0.0.7

### Active

No active requirements — defining requirements for v4.0.0.8 Notification Boundary.

### Deferred (future milestones)

- [ ] TEST-04: Maintainer can verify key Web or Blazor components with dedicated component tests
- [ ] TEST-05: Maintainer can enforce focused coverage thresholds for critical modules in CI
- [ ] FLOW-05: Maintainer can unify Console and Web configuration and startup composition paths where behavior meaningfully overlaps
- [ ] QUAL-03: Maintainer can remove default credential risks and similar obvious safety issues from bootstrap flows
- [ ] QUAL-04: Maintainer can reduce repository noise from generated outputs so searches and reviews focus on source of truth files

### Out of Scope

- Rewriting the product from scratch — the goal is to improve the existing system, not replace it
- Large feature expansion unrelated to maintainability — this work is about structural improvement first
- UI redesign as a primary goal — web cleanup may happen where needed, but visual redesign is not the main objective

## Context

- The repository is a multi-project .NET 8 solution centered on `Ray.BiliBiliTool.sln`
- Executable surfaces include `src\Ray.BiliBiliTool.Console`, `src\Ray.BiliBiliTool.Web`, and `src\Ray.BiliBiliTool.Web.Client`
- v4.0.0.1 shipped 2026-05-03: 6 phases, 13 plans, 128 files changed, 8707 insertions, 264 deletions
- v4.0.0.2 shipped 2026-05-04: 1 phase, 4 plans — cookie-handling centralized in `BaseMultiAccountsAppService`; all 11 AppServices migrated; DiagnosticScope telemetry added to all
- v4.0.0.3 shipped 2026-05-04: Refit Migration — WebApiClientCore fully replaced by Refit 8.0.0 across all 18 Agent interfaces; 27 src files changed (+306/−460)
- v4.0.0.6 shipped 2026-05-05: Web Layer Boundary Cleanup — 4 phases (13–16), 9 plans, 67 files changed (+5824/−494); 5 Web-layer workflow seams introduced; `Ray.BiliBiliTool.Web.ComponentTests` project with 28 bUnit tests; ArchUnit Web.Components guardrail; all 6 WEB requirements satisfied
- v4.0.0.7 shipped 2026-05-07: Bili Account Management — 3 phases (17–19), 4 plans, 36 files changed (+2965/−28); Web-based account CRUD with QR login; SQLite-backed cookie storage with cookies.json fallback; 7/7 ACCT requirements satisfied
- Architecture guardrails (ArchUnitNET) enforce layer direction across Agent, Application, DomainService, Infrastructure, and Web — 5/5 tests passing
- Characterization and integration test harnesses now freeze Login and DailyTask flows across `test\Ray.BiliBiliTool.Test.Characterization` and `test\Ray.BiliBiliTool.Test.Integration`
- All 12 Quartz job classes are thin expression-body delegation shells; orchestration lives in LoginTaskAppService and DailyTaskAppService
- BiliException hierarchy (Business/Integration/Validation) established in `src\Ray.BiliBiliTool.Domain\Exceptions`
- IExecutionLogRepository and IUserRepository adapters decouple Web from direct EF factory injection
- `BaseMultiAccountsAppService` now owns `SetCookiesAsync` + `SaveCookieAsync` as `protected virtual` — all 11 in-scope services inherit; `LoginTaskAppService` retains its own (out of scope by design)
- Notification boundary not yet established — 13 Serilog sink channels active (Console, File, Telegram, WorkWeChat×2, DingTalk, ServerChan, CoolPush, OtherApi, PushPlus, Teams, Gotify); no `INotificationService` abstraction; `BatchSinkManager.FlushAsync()` triggers at job end — target of v4.0.0.8

## Constraints

- **Brownfield**: Existing behavior must be preserved while restructuring — this is a live codebase with validated capabilities
- **Incremental Delivery**: Refactor work must be phaseable and low-risk — a big-bang rewrite would increase regression risk
- **Compatibility**: Existing Console/Web/task workflows should keep working during transition — operational continuity matters
- **Testing**: New boundaries should be introduced alongside verifiable tests — otherwise refactors remain unsafe
- **Maintainability**: Refactor decisions should favor simpler dependency direction and clearer ownership — reducing future change cost is the primary purpose

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Treat this as a brownfield refactor project, not a new product | The user wants to improve the current system rather than replace it | Confirmed — v4.0.0.1 shipped without breaking existing flows |
| Prioritize architecture boundaries, code quality, and testability first | These are the main pain points blocking safe changes today | Confirmed — ArchUnitNET guardrails + test harnesses established |
| Use gradual, phase-based refactoring instead of a rewrite | The user explicitly wants low-risk incremental change | Confirmed — 6 phases with atomic commits |
| ArchUnitNET for executable dependency enforcement | Provides CI-enforced compile-time-adjacent guardrail without custom tooling | Shipped Phase 1 |
| TaskFlowDiagnosticScope for flow comparison | Enables comparing old vs. refactored critical paths through structured log markers | Shipped Phase 2 |
| BiliException hierarchy (Business/Integration/Validation) | Enables distinguishable failure modes across DomainService and Agent layers | Shipped Phase 6 |
| ARCH-04 notification boundary deferred | Current Serilog sink works; establishing an explicit port adds scope without urgent payoff | Deferred beyond v4.0.0.2 |
| Extract shared AppService cookie handling into base class | 6 AppServices copy identical SetCookie/SaveCookie private methods — DRY violation targets a single base class | v4.0.0.2 Phase 7 |
| Keep cookies.json as fallback (not remove) in Web host | Existing Console-host users who haven't migrated to SQLite would break; fallback loaded before AddSqlite so SQLite wins for overlapping keys | v4.0.0.7 Phase 17 |
| QR login generates base64 PNG in domain service layer | Web browser displays PNG via img tag; terminal QR rendering doesn't apply; PngByteQRCode already referenced in DomainService.csproj | v4.0.0.7 Phase 19 |
| INotificationService port for notification boundary | All 13 current channels are Serilog sinks with no application-level abstraction; port enables future non-Serilog adapters (Telegram HTTP, webhook, etc.) without touching callers | v4.0.0.8 (active) |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-05-07 — v4.0.0.8 milestone started (Notification Boundary)*