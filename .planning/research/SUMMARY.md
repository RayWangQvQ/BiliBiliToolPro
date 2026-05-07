# Research Summary — v4.0.0.8 Notification Boundary

**Project:** BiliBiliToolPro
**Domain:** Brownfield notification port/adapter boundary extraction for .NET 8 task automation app
**Researched:** 2026-05-07
**Confidence:** HIGH

## Executive Summary

BiliBiliToolPro currently sends all notifications through `ILogger` → Serilog pipeline → 13 sink channels (Telegram, WorkWeChat, DingTalk, etc.) with batch-at-end semantics via `BatchSinkManager.FlushAsync()`. There is no application-level notification abstraction — diagnostic logs and user-facing notifications are the same data stream. The v4.0.0.8 milestone introduces an `INotificationService` port/adapter boundary to separate these concerns.

The research reveals a favorable situation: **zero new runtime NuGet packages are needed**. The Telegram Bot API is a single HTTP POST endpoint, handled natively by the already-referenced `IHttpClientFactory`. Configuration binding uses the established `IOptionsMonitor<T>` pattern. The port/adapter placement follows existing project conventions exactly — port in `Application.Contracts`, adapters in `Infrastructure`. The primary risk is **behavioral**: preserving the batch-flush timing model while introducing a dual-path (Serilog + native HTTP) notification flow without duplicate messages or lost `fireInstanceId` grouping context.

The recommended approach is incremental: define the port first, add the Serilog zero-breakage adapter second, then prove extensibility with the native Telegram HTTP adapter last. Each phase is independently verifiable and preserves all 13 existing channels untouched until explicitly migrated.

## Key Findings

### Stack Additions

**New runtime packages: NONE.** Everything needed is already in the project.

| Concern | Technology | Status |
|---------|-----------|--------|
| Port interface | `INotificationService` (project-owned) | Create in `Application.Contracts` |
| HTTP client | `IHttpClientFactory` via `Microsoft.Extensions.Http` 8.0.1 | Already in `Directory.Packages.props` |
| JSON serialization | `System.Text.Json` (BCL) | Already available |
| Configuration binding | `IOptionsMonitor<T>` + `GetSection()` | Already the convention |
| Logging passthrough | `ILogger<T>` | Already everywhere |

**Test-only addition:**
- `NSubstitute` 5.3.0 — for mocking `INotificationService` in unit tests (add to `Directory.Packages.props`)

### Feature Table Stakes vs Differentiators

**Must have (table stakes):**
- `INotificationService` port interface — the entire milestone goal
- Serilog adapter (zero-breakage) — 13 existing channels must keep working identically
- Telegram HTTP adapter — proves the port enables non-Serilog extensibility
- Configuration-driven channel selection — existing Serilog config untouched; new Telegram config section
- Batch-at-end semantics preserved — `BatchSinkManager.FlushAsync()` timing contract unchanged

**Should have (differentiators):**
- Format abstraction (PlainText/Html/Markdown enum on DTO) — low cost to add now, full adapter implementation later
- Notification severity levels (`NotificationLevel` enum: Info/Warning/Error) — maps to Telegram silent/audible, Serilog log levels
- Telegram `retry_after` handling — respects 429 rate limit responses
- Message truncation for 4096-char Telegram limit

**Defer (v2+):**
- Conditional channel routing ("errors only to Telegram") — not needed for proof-of-concept
- Notification templating from structured data — raw ILogger messages are sufficient
- Telegram MarkdownV2 formatting with escaping — free once adapter is built, but not MVP
- Multi-account notification targets — future concern
- Webhook receiver / bidirectional notifications — completely different feature

### Architecture Integration

**Port placement: `Application.Contracts`** — follows existing `IAppService` convention. All layers can reference it. No new project needed.

**Adapter placement: `Infrastructure`** — follows existing Agent adapter pattern. Two classes + one options POCO. No new project needed.

**Components to create:**

| Component | Layer | Responsibility |
|-----------|-------|----------------|
| `INotificationService` | Application.Contracts | Port interface: `SendAsync` + `SendSummaryAsync` |
| `NotificationMessage` | Application.Contracts | Value object: Title, Body, Level, GroupKey |
| `SummaryLine` | Application.Contracts | Value object: Label, Value, Status |
| `NotificationLevel` | Application.Contracts | Enum: Info, Warning, Error |
| `SerilogNotificationAdapter` | Infrastructure | Default adapter → `ILogger.LogInformation` → existing pipeline |
| `TelegramHttpAdapter` | Infrastructure | Native HTTP POST to Telegram Bot API |
| `CompositeNotificationAdapter` | Infrastructure | Fan-out to multiple adapters |
| `TelegramNotificationOptions` | Infrastructure | Config POCO: botToken, chatId, apiHost, proxy |

**Data flow (after):**
```
AppService step
    ├── logger.LogInformation(...)        ← diagnostic (unchanged, all 13 sinks)
    └── notificationService.SendAsync()   ← NEW: port
            ├── SerilogNotificationAdapter  → logger.LogInformation("[Notification]...")
            └── TelegramHttpAdapter        → POST api.telegram.org (independent of Serilog)

Job end → BatchSinkManager.FlushAsync()   ← UNCHANGED, still flushes Serilog
```

**Key insight:** `SerilogNotificationAdapter` writes via `ILogger`, entering the same Serilog pipeline. `BatchSinkManager` still groups by `fireInstanceId` and flushes at job end. Zero breaking change.

### Critical Pitfalls (Top 5)

1. **Breaking the batch flush model** — If Telegram adapter fires immediately while Serilog sinks defer to flush, behavior diverges. **Prevention:** Define batching semantics explicitly in the port contract. The Serilog adapter defers (existing behavior); native adapters can fire immediately or batch internally.

2. **Duplicate Telegram messages** — If native Telegram adapter is registered alongside existing Serilog Telegram sink, users get every notification twice. **Prevention:** When native adapter is configured, auto-disable the Serilog `TelegramBatched` sink. Config-driven: if `Notification:Telegram` section exists, suppress the Serilog Telegram sink.

3. **Circular dependency through ILogger** — Adapter error → logged via ILogger → hits Serilog notification sinks → triggers adapter → infinite loop. **Prevention:** Use `Log.ForContext("Notification", true)` in Serilog adapter; configure sinks to exclude `NotificationOrigin=adapter` tagged events.

4. **Over-abstraction** — Growing port layer to 5+ interfaces. **Prevention:** YAGNI — start with one interface, one DTO. Extract only when three implementations genuinely share behavior.

5. **Config drift** — Users configure Telegram in `Serilog:WriteTo:TelegramBatched` AND `Notification:Telegram` with different values. **Prevention:** Read from existing Serilog config as fallback if new section absent. Warn on conflict at startup.

## Implications for Roadmap

### Phase 1: Port Definition & Contracts

**Rationale:** The interface shape determines everything downstream — batching semantics, DTO structure, severity model. Must be right before any adapter is built.

**Delivers:**
- `INotificationService` interface in `Application.Contracts/Notifications/`
- `NotificationMessage`, `SummaryLine`, `NotificationLevel` DTOs
- ArchUnit rule: Application layer must not reference Infrastructure.Notifications directly

**Addresses:** Table stake #1 (port interface), differentiator (severity enum, format enum on DTO)

**Avoids:** Pitfall 1 (batch flush semantics baked into contract), Pitfall 4 (YAGNI constraint on interface surface)

### Phase 2: Serilog Notification Adapter

**Rationale:** Zero-breakage bridge to existing 13 channels. Must work before any native adapter to prove the port doesn't break the existing pipeline.

**Delivers:**
- `SerilogNotificationAdapter` in `Infrastructure/Notifications/`
- Default DI registration as `INotificationService`
- Characterization test: notification delivery timing matches pre-refactor behavior
- Verify `BatchSinkManager.FlushAsync()` still works with adapter in the path

**Addresses:** Table stake #2 (zero-breakage Serilog adapter), table stake #5 (batch-at-end preserved)

**Avoids:** Pitfall 3 (circular dependency — Log.ForContext pattern), Pitfall 8 (fireInstanceId context handling)

### Phase 3: Telegram HTTP Adapter

**Rationale:** Proves the port enables non-Serilog extensibility. Depends on port (Phase 1) and DI wiring (Phase 2).

**Delivers:**
- `TelegramHttpAdapter` in `Infrastructure/Notifications/`
- `TelegramNotificationOptions` config POCO
- Rate limiting (SemaphoreSlim, 25 msg/sec headroom)
- `retry_after` handling for 429 responses
- 4096-char message truncation
- Proxy support (matching existing `TelegramBatched` sink config)
- Duplicate prevention: auto-disable Serilog Telegram sink when native adapter configured

**Addresses:** Table stake #3 (Telegram HTTP adapter), table stake #4 (config-driven selection), differentiators (retry, truncation)

**Avoids:** Pitfall 2 (duplicate messages), Pitfall 5 (config drift — read from existing Serilog config as fallback), Pitfall 6 (rate limiting)

### Phase 4: DI Wiring & AppService Migration

**Rationale:** Wire everything together and route notification-worthy signals through the port. Depends on all adapters being stable.

**Delivers:**
- `AddNotificationServices()` extension method in Infrastructure
- `builder.Services.AddNotificationServices()` call in both Web and Console hosts
- `appsettings.json` updates for both hosts (new `Notification:Telegram` section)
- `BaseMultiAccountsAppService`: optional `INotificationService?` parameter, summary call after all accounts
- Selective AppService migration: key result points routed through port (1-2 calls per service)
- All existing `ILogger` calls untouched

**Addresses:** Table stake #4 (config-driven selection), migration goal (task summaries through port)

**Avoids:** Pitfall 7 (ArchUnit layer direction — port in Contracts, adapter in Infrastructure, DI in host)

### Phase Ordering Rationale

1. **Port first** because interface shape is the foundation — batching semantics, DTO structure, and severity model constrain all downstream work.
2. **Serilog adapter second** because it proves the port doesn't break existing behavior — zero-breakage validation before adding new channels.
3. **Telegram adapter third** because it proves extensibility — the core value proposition of the port/adapter pattern.
4. **Migration last** because it's the highest-touch change (modifying 11 AppServices) and should only happen after the adapter infrastructure is stable.

### Research Flags

Phases with standard patterns (skip research-phase):
- **Phase 1:** Interface + DTOs — straightforward .NET interface design, well-documented patterns
- **Phase 2:** Serilog adapter — thin ILogger wrapper, project already has extensive Serilog integration

Phases that may benefit from deeper research during planning:
- **Phase 3:** Telegram adapter — rate limiting strategy, proxy format validation, MarkdownV2 escaping edge cases
- **Phase 4:** AppService migration — need to audit each of 11 services to identify which log calls are "notification-worthy" vs purely diagnostic

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | **HIGH** | All dependencies verified against `Directory.Packages.props` and existing csproj files. Zero new runtime packages needed. |
| Features | **HIGH** | Table stakes defined from codebase analysis (13 sinks, BatchSinkManager, existing config). Telegram specifics from official Bot API docs (March 2026). |
| Architecture | **HIGH** | Layer placement follows existing `IAppService` + Agent adapter patterns exactly. ArchUnitNET rules already enforce direction. `fireInstanceId` batch routing verified from `BaseJob.cs` source. |
| Pitfalls | **HIGH** | All 8 pitfalls derived from specific codebase integration points (not generic advice). Duplicate message risk verified from existing Serilog Telegram sink config. |

**Overall confidence:** HIGH

### Gaps to Address

- **NSubstitute version:** Verify latest stable version at implementation time. `Directory.Packages.props` uses centrally managed versions.
- **Proxy format for Telegram adapter:** Existing `TelegramBatched` sink accepts `user:password@host:port`. Verify exact format and `HttpClientHandler.Proxy` configuration.
- **Which AppService log calls are "notification-worthy":** Phase 4 needs per-service audit to identify 1-2 key calls per service vs purely diagnostic logs. This is an execution-time decision, not a research gap.
- **Serilog Telegram sink auto-disable mechanism:** Need to confirm how to programmatically disable a specific Serilog sink at startup when the native adapter is registered (may require Serilog pipeline modification or config override).

## Sources

### Primary (HIGH confidence)
- Telegram Bot API: https://core.telegram.org/bots/api#sendmessage — endpoint, rate limits, parse modes, 4096 char limit
- `Directory.Packages.props` — all NuGet versions, 13 `Ray.Serilog.Sinks.*Batched` packages
- `src/Ray.BiliBiliTool.Web/Jobs/BaseJob.cs` — `BatchSinkManager.FlushAsync(fireInstanceId)` + `LogContext.PushProperty` pattern
- `src/Ray.BiliBiliTool.Config/Extensions/ServiceCollectionExtension.cs` — `IOptionsMonitor<T>` binding pattern
- `src/Ray.BiliBiliTool.Agent/Extensions/ServiceCollectionExtension.cs` — `IHttpClientFactory` + Refit registration
- `.planning/PROJECT.md` — milestone context, constraints, architecture guardrails

### Secondary (MEDIUM confidence)
- `test/LogTest/TestTelegram.cs` — existing `TelegramApiClient` usage from Serilog sink package (proxy format reference)
- `.planning/codebase/CONVENTIONS.md` — DI, HTTP client, and configuration conventions

---
*Research completed: 2026-05-07*
*Ready for roadmap: yes*
