# Feature Landscape — Notification Port/Adapter Boundary

**Domain:** Notification port/adapter boundary for .NET 8 task automation app
**Researched:** 2026-05-07
**Overall confidence:** HIGH (codebase fully analyzed; Telegram Bot API verified from official docs)

## Current State Summary

Today, notifications flow through `ILogger` → Serilog sinks → `BatchSinkManager.FlushAsync()` at job end. There are 13 active sink channels (Console, Debug, File, Telegram, WorkWeChat×2, DingTalk, ServerChan, CoolPush, OtherApi, PushPlus, Teams, Gotify), all as custom `Ray.Serilog.Sinks.*Batched` NuGet packages. Each sink internally uses `PushMessageAsync(msg, title?)`. Configuration lives in `appsettings.json` under `Serilog:WriteTo`. There is no application-level notification abstraction — the port boundary must be built from scratch.

## Table Stakes

Features users expect. Missing = product feels incomplete or broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **`INotificationService` port interface** | The entire milestone goal — without this, nothing else matters | Low | Single interface in `Application.Contracts` with `SendAsync(NotificationMessage)` or similar. Must be async. |
| **Serilog adapter (zero-breakage)** | 13 existing channels must keep working identically after migration | Medium | Adapter delegates to existing `ILogger` calls so Serilog sinks continue to batch and flush. Must preserve `GroupPropertyKey` / `BatchSinkManager.FlushAsync()` timing. |
| **Telegram HTTP adapter** | Required by milestone to prove the port enables non-Serilog extensibility | Medium | Native `HttpClient` POST to `https://api.telegram.org/bot{token}/sendMessage`. Must support `botToken`, `chatId`, optional `proxy`, optional `apiHost` (custom reverse proxy) — mirroring existing `TelegramBatched` sink config. |
| **Configuration-driven channel selection** | Users configure which channels are active via `appsettings.json`; this must continue | Low | Serilog adapter inherits existing config. Telegram HTTP adapter needs its own config section (e.g., `Notification:Telegram`). |
| **Batch-at-end semantics preserved** | Current behavior: log during job → `BatchSinkManager.FlushAsync()` at job end sends all buffered notifications. Users depend on this. | Medium | Serilog adapter must not interfere with existing flush timing. Telegram HTTP adapter can either batch internally or send immediately — batch-at-end is the safer default for compatibility. |

## Differentiators

Features that set the port apart. Not expected, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Format abstraction (plain/HTML/Markdown)** | Different channels support different formats. Telegram supports HTML and MarkdownV2. DingTalk supports Markdown. Serilog sinks currently get raw text. | Medium | `NotificationMessage` could carry a `Format` enum (PlainText, Html, Markdown). Serilog adapter ignores format (sinks handle their own rendering). Telegram adapter maps to `parse_mode`. |
| **Per-channel format override** | A Telegram adapter can send MarkdownV2 while a DingTalk adapter sends plain text from the same message | Medium | Requires each adapter to declare its preferred format, or config to specify per-channel format. Adds adapter-level intelligence. |
| **Notification severity / priority levels** | Not all notifications are equal. An error summary should be more prominent than a routine "task complete" | Low | Add `NotificationLevel` enum (Info, Warning, Error) to `NotificationMessage`. Telegram adapter maps to `disable_notification` (silent for Info, audible for Error). Serilog adapter maps to log levels. |
| **Conditional channel routing** | "Only send errors to Telegram, send all to DingTalk" — reduces noise | Medium | Routing rules in config: `Notification:Rules[]` with severity filters per channel. Adds a routing layer between port and adapters. Defer unless time permits. |
| **Retry with exponential backoff** | Network calls to Telegram/DingTalk can fail transiently | Medium | Telegram adapter should retry 429 (rate limit) with `retry_after` from response. Polly already used in the project for HTTP. Serilog adapter inherits sink-level retry. |
| **Notification message templating** | Structured templates for "Daily task completed" vs "Error in task X" instead of raw log text | High | A templating layer that builds `NotificationMessage` from structured data (task name, results, errors). Valuable but adds significant scope. Defer. |
| **Telegram-specific: MarkdownV2 formatting** | Rich formatting with bold, italic, code blocks, inline links | Low | Telegram adapter sets `parse_mode: "MarkdownV2"` and escapes special characters. Free once the adapter is built. |
| **Telegram-specific: reply keyboard / inline buttons** | Allow interactive notifications (e.g., "retry" button) | High | Requires webhook/callback infrastructure. Out of scope for this milestone. |
| **Multi-account notification targets** | The app runs tasks for multiple Bili accounts; notification should optionally target per-account or global | Low–Med | Port method can accept an optional account identifier. Serilog adapter ignores it (batch sink is global). Telegram adapter can route to different chatIds if configured. |

## Anti-Features

Features to explicitly NOT build.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Real-time / push notifications** | Current architecture batches at job end; changing to real-time would break the `BatchSinkManager.FlushAsync()` contract and require rethinking every sink | Keep batch-at-end semantics. If real-time is ever needed, add it as a separate `INotificationService` implementation, not a modification of the port. |
| **Custom message queue / event bus** | Massively over-engineered for a task automation tool that runs on schedule. No pub/sub, no message broker. | Use direct `await notificationService.SendAsync(message)` calls. If fan-out to multiple adapters is needed, a simple composite adapter (calls N adapters in sequence) is sufficient. |
| **Notification history / persistence** | Storing sent notifications in DB adds schema, queries, UI — pure scope creep for this milestone | Log notifications via `ILogger` (already happens). If history is needed later, add a logging adapter. |
| **User-facing notification configuration UI** | Web UI for enabling/disabling channels, setting thresholds, etc. | Configuration stays in `appsettings.json`. This matches all existing Serilog sink config patterns and requires zero new UI code. |
| **Rate limiting in the port layer** | Each external API (Telegram, DingTalk, etc.) has different rate limits. Building generic rate limiting into the port is fragile. | Let each adapter handle its own rate limiting. Telegram adapter uses `retry_after` from 429 responses. Serilog sinks already have their own batching/throttling. |
| **Notification templates in the port** | Template engines, variable substitution, localization — all add scope | Keep `NotificationMessage` as a simple DTO with `Title`, `Body`, `Format`. Adapters can format as needed. Templating is a future concern. |
| **Webhook receiver / bidirectional notifications** | "Bot receives commands from Telegram and acts on them" — completely different concern | This is a bot framework, not a notification system. If ever needed, it's a separate feature entirely. |

## Feature Dependencies

```
INotificationService port (Application.Contracts)
    ├── Serilog adapter (Infrastructure) ──→ existing Ray.Serilog.Sinks.* packages
    │       └── depends on: BatchSinkManager, GroupPropertyKey, ILogger
    └── Telegram HTTP adapter (Infrastructure) ──→ HttpClient (Polly already configured)
            └── depends on: Telegram Bot API, Notification:Telegram config section
```

## Telegram Bot API Specifics

Verified from official docs (https://core.telegram.org/bots/api, Bot API 9.5, March 2026).

| Aspect | Detail | Impact on Feature |
|--------|--------|-------------------|
| **Endpoint** | `POST https://api.telegram.org/bot{token}/sendMessage` | Standard HttpClient POST with JSON body |
| **Required params** | `chat_id` (int or string), `text` (1–4096 chars) | `NotificationMessage.Body` must be ≤ 4096 chars. Truncation logic needed for long task summaries. |
| **Parse modes** | `MarkdownV2`, `HTML`, `Markdown` (legacy) | Map from `NotificationMessage.Format`. MarkdownV2 requires escaping `_`, `*`, `[`, `]`, `(`, `)`, `~`, `` ` ``, `>`, `#`, `+`, `-`, `=`, `|`, `{`, `}`, `.`, `!` |
| **Message length** | 4096 chars max after entity parsing | Batch of log messages may exceed this. Need chunking or truncation strategy. |
| **Rate limits** | 30 msgs/sec to different chats; 1 msg/sec to same chat. 429 with `retry_after` in response. | Telegram adapter should respect `retry_after`. For a single `chatId`, effectively 1 msg/sec. |
| **Proxy support** | Existing sink supports `proxy` config (`user:password@host:port`) | Telegram HTTP adapter must accept proxy config and configure `HttpClient` handler accordingly. |
| **Custom API host** | Existing sink supports `apiHost` for reverse proxies | Telegram HTTP adapter must accept `apiHost` config (default: `https://api.telegram.org`). |
| **Silent messages** | `disable_notification: true` sends without sound | Map from `NotificationLevel.Info` → silent, `NotificationLevel.Error` → audible. |
| **Response format** | `{ "ok": true, "result": { ... } }` or `{ "ok": false, "description": "...", "error_code": 429, "parameters": { "retry_after": N } }` | Parse response to detect rate limits and errors. |
| **MarkdownV2 escaping** | Special chars must be be escaped with `\` inside/outside entities | Need a `TelegramMarkdownEscaper` utility. Common source of bugs — test thoroughly. |

## MVP Recommendation

Prioritize:
1. **`INotificationService` port** — foundation of everything
2. **Serilog adapter** — zero-breakage migration of existing 13 channels
3. **Telegram HTTP adapter** — proves extensibility; mirror existing sink config
4. **Configuration-driven channel selection** — inherits from Serilog + new Telegram section
5. **Batch-at-end semantics** — preserve `FlushAsync()` timing contract

Defer:
- **Format abstraction**: Serilog adapter passes raw text (existing behavior). Telegram adapter defaults to plain text. Format enum added to DTO but not exercised by adapters yet. Low cost to add the enum now, full implementation later.
- **Conditional channel routing**: Not needed for proof-of-concept. Single routing rule "all notifications → all active channels" matches current behavior.
- **Notification templating**: Raw `ILogger` messages become the notification body. Templating is a future concern.
- **Retry with backoff**: Telegram adapter should handle 429 retry_after (low cost). Full Polly retry policies for all adapters deferred.

## Sources

- Telegram Bot API: https://core.telegram.org/bots/api — HIGH confidence (official docs, verified March 2026)
- Existing codebase: `BaseJob.cs`, `appsettings.json`, `Ray.Serilog.Sinks.*` packages, `BatchSinkManager` — HIGH confidence (direct source analysis)
- Existing test files: `test/LogTest/TestTelegram.cs`, `TestDingTalk.cs`, etc. — HIGH confidence (existing integration patterns)
|---|---|---|
| Outcome alignment and success metrics | Brownfield refactors fail when teams optimize for "cleaner code" instead of faster, safer change. Define target outcomes such as lead time, defect rate, hot spots, and time-to-test. | Lock a small scorecard before moving code. Review it every milestone. |
| Current-state architecture map | Mature systems usually hide coupling in schedulers, shared helpers, configuration, and data access. A thin dependency map and runtime flow map are mandatory. | Map only the critical flows first: login/session, scheduled task execution, external API calls, persistence. |
| Seam identification | Successful programs create seams before they extract modules. Typical seams are interfaces, adapters, event/router points, repository boundaries, and composition roots. | Introduce seams with behavior-preserving refactors first. No logic moves in the same change if avoidable. |
| Characterization test suite for critical flows | Weak tests are the main brownfield tax. Characterization tests freeze behavior so refactors can proceed safely. | Start with observable end-to-end or slice tests around the highest-risk flows, not broad unit-test campaigns. |
| Dependency rule enforcement | Boundary rules must become executable or they decay immediately. | Add architecture tests or build checks early; fail new violations first, then ratchet down debt. |
| Transitional architecture plan | Temporary adapters, facades, and anti-corruption layers are normal in successful refactors. Pretending they are unnecessary drives big-bang changes. | Track each temporary component with a removal condition and owner. |
| Incremental rollout and fallback strategy | Every major change needs a reversible release path. | Prefer branch-by-abstraction, feature toggles, side-by-side implementations, and small cutovers. |
| Observability for refactor paths | Teams need to see whether the new path behaves like the old one. Logs, traces, counters, and diff checks are mandatory. | Instrument before switching traffic. Keep old vs new comparison data during rollout. |
| Delivery operating model | Refactor work cannot be separated from normal feature delivery for long. It needs explicit capacity and governance. | Reserve a stable percentage of each milestone for enabling work and debt retirement. |
| Exit criteria per slice | Each slice needs a definition of done stronger than "code moved." | Require tests, dependency compliance, observability, docs, and a rollback path before declaring a slice complete. |

## High-Leverage Improvements

These are not the absolute minimum, but they strongly increase the odds that a modular-monolith refactor finishes with lasting gains.

| Improvement / Deliverable | Why it pays off | Suggested use |
|---|---|---|
| Change hotspot analysis | Helps choose the first refactor slices based on churn, incidents, and dependency pain rather than intuition. | Use commit history and bug history to rank flows before planning milestones. |
| Module scorecards | Makes "clearer boundaries" measurable through dependency count, public surface area, test coverage on critical paths, and ownership clarity. | Review per module at milestone boundaries. |
| Golden-master or snapshot protection for legacy payloads | Useful when outbound API payloads, config shapes, or scheduler orchestration are too awkward for many small assertions. | Apply only to payload-heavy or especially fragile flows. |
| Side-by-side verification harness | Lets old and new implementations run in parallel and compare results before full cutover. | Use for external API clients, orchestration services, and calculation-heavy paths. |
| Module ownership and decision log | Boundary clarity is organizational as much as technical. Teams need to know who owns which contracts. | Record module intent, allowed dependencies, and open exceptions. |
| Refactor-safe developer workflow | Fast local test loops, targeted integration tests, and consistent review checklists reduce regression risk. | Standardize a small review checklist for seam changes and dependency changes. |
| Deferred cleanup backlog tied to seams | Transitional code tends to linger unless cleanup is planned at the time it is introduced. | Create explicit teardown tickets whenever temporary adapters or toggles are added. |
| Fitness functions in CI | Prevents backsliding after the first cleanup wave. | Automate checks for layering rules, forbidden references, and key test suites. |

## Anti-Features

These repeatedly show up in failed brownfield programs and should be excluded from the roadmap.

| Anti-feature | Why to avoid it | Better alternative |
|---|---|---|
| Big-bang rewrite milestone | Maximizes scope, delays feedback, and removes rollback options. | Break work into thin vertical slices with live coexistence. |
| Full feature parity as a prerequisite | Teams spend months reproducing accidental legacy behavior and hidden edge cases. | Preserve only behavior required by critical flows and observable contracts. |
| Framework swap as the main objective | Replacing the stack rarely fixes coupling or weak tests by itself. | Keep platform choices stable while improving boundaries and tests first. |
| Repository-wide test rewrite | Consumes time without protecting the riskiest areas soon enough. | Start with characterization and slice tests around high-value flows. |
| Massive namespace/project reshuffle upfront | Produces noise and merge pain before boundaries are enforceable. | Introduce dependency rules, then move one slice at a time. |
| Shared "common" expansion | Central utility layers often become new coupling magnets. | Prefer explicit module contracts and narrowly scoped adapters. |
| Long-lived parallel architecture with no retirement plan | Temporary code becomes permanent and doubles maintenance cost. | Add removal triggers and deadline-based cleanup reviews. |
| Microservices split during boundary cleanup | Distributed complexity hides the real problem and slows delivery. | Prove modular boundaries inside the monolith first. |
| Refactor-only branch that diverges from trunk | Delays validation against real change pressure. | Keep changes incremental on trunk with toggles or abstraction seams. |

## Dependency Notes

- Suggested dependency order: outcomes and metrics -> flow map -> seam creation -> characterization tests -> dependency enforcement -> first extraction/cutover.
- Observability should precede any traffic switch. If old and new paths cannot be compared, the rollout is not ready.
- Transitional architecture is a dependency, not a smell, when it creates safe coexistence and rollback.
- Organizational ownership must follow boundary work. A module without a clear owner usually regresses.
- Each new seam should unlock at least one of three things: testing, observation, or traffic redirection.
- Critical reports, scheduled jobs, and external integrations often depend on hidden data paths; treat them as first-class discovery items before extraction.
- In a production-like codebase, "done" means reversible in deployment terms, not only compilable in source terms.

## Suggested First Milestone Outcomes

Aim for one milestone that proves the refactor program can deliver safer change, not one that claims to finish the architecture.

1. A published refactor scorecard with 3-5 measurable outcomes tied to cost of change.
2. A current-state map of the top 2-3 critical flows and their hidden dependencies.
3. Characterization tests around at least one critical orchestration path and one external integration path.
4. One enforced dependency rule set that blocks new boundary violations in CI.
5. One explicit seam introduced in a high-churn area with no intended behavior change.
6. Basic observability added for the chosen slice so old vs new execution can be compared.
7. A thin transitional architecture decision for the first extraction path, including rollback and removal criteria.
8. A ranked backlog of follow-on slices based on hotspot data, operational risk, and team readiness.

Bottom line: the roadmap should treat tests, seams, dependency rules, observability, and rollout safety as product-grade deliverables. For a brownfield modular monolith, those are the actual features that make future delivery faster.

Sources informing this summary: Martin Fowler on Strangler Fig, Legacy Seam, Branch by Abstraction, and Thoughtworks' Patterns of Legacy Displacement, especially transitional architecture and incremental delivery.