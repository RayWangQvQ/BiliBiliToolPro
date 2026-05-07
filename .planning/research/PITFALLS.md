# Pitfalls Research: Notification Port/Adapter Boundary

**Domain:** Brownfield notification boundary extraction for .NET 8 with 13 existing Serilog sink channels
**Researched:** 2026-05-07
**Confidence:** HIGH

> **Previous version:** Generic brownfield refactor pitfalls (v4.0.0.1 era). Replaced with notification-boundary-specific pitfalls for v4.0.0.8.

---

## Critical Pitfalls

### Pitfall 1: Breaking the Batch Flush Model

**What goes wrong:**
The existing system batches notification messages per job run using `LogContext.PushProperty(Constants.GroupPropertyKey, fireInstanceId)`. All 13 `Ray.Serilog.Sinks.*Batched` sinks collect messages grouped by `fireInstanceId`, then `BatchSinkManager.FlushAsync(fireInstanceId)` fires the HTTP calls to all channels at once when the job ends (`BaseJob.cs:47`). If the new `INotificationService` adapter calls the Telegram API (or any channel) immediately at send time rather than batching and flushing at job end, it creates a dual-path model where:
- The 13 Serilog sinks still batch + flush at job end (correct)
- The new native Telegram adapter fires immediately (different behavior, confusing)
- Some channels get duplicate messages if both Serilog and native adapters route the same notification

**Why it happens:**
Developers naturally think of `INotificationService.SendAsync()` as an immediate-send API. They build the Telegram adapter as a simple HTTP POST per call without realizing the entire existing pipeline depends on deferred batch flush. The port interface looks like it should fire-and-forget, but the Serilog adapter underneath still batches.

**How to avoid:**
- The `INotificationService` port interface must make batching semantics explicit. Either:
  - **(a) Recommended:** Define `SendAsync(message)` as "enqueue for this job's group" plus `FlushAsync(groupId)` as a separate method — mirrors the existing `BatchSinkManager` model exactly. The Serilog adapter just forwards to `ILogger`, and `BatchSinkManager` handles the rest unchanged.
  - **(b)** Define `SendAsync(message)` as truly immediate, and have the Serilog adapter internally buffer until flush — but document this clearly so native adapters know they fire immediately while Serilog adapters defer.
- Option (a) is safer for brownfield: minimal behavioral change, native adapters can implement their own batching or fire immediately per their own semantics.
- **Critical test:** After adding the port, run a DailyTask end-to-end. Verify Telegram messages arrive at the same point in the job lifecycle as before (at flush time, not at log time).

**Warning signs:**
- Telegram messages arrive mid-job instead of at job completion
- Duplicate notifications (once from Serilog TelegramSink, once from native Telegram adapter)
- Characterization tests for DailyTask notification timing fail

**Phase to address:**
Phase 1 (Port definition) — the interface shape determines everything downstream.

---

### Pitfall 2: Dual-Write / Duplicate Notifications During Migration

**What goes wrong:**
During migration, both the old Serilog Telegram sink (`Ray.Serilog.Sinks.TelegramBatched`) and the new native Telegram adapter are active. Every notification goes to Telegram twice: once through the Serilog pipeline (which still has Telegram configured), and once through the native adapter. Users get duplicate messages and disable the feature.

**Why it happens:**
The project spec says "Serilog adapter routes notification calls through existing 13 sinks" — but the existing Telegram Serilog sink is one of those 13. If you add a native Telegram adapter *alongside* the existing Serilog Telegram sink (not replacing it), Telegram gets two copies of every message.

**How to avoid:**
- **Option A (recommended):** When the native Telegram adapter is registered, automatically remove `Ray.Serilog.Sinks.TelegramBatched` from the Serilog pipeline. The native adapter replaces exactly that one sink. The other 12 sinks remain on the Serilog path.
- **Option B:** Make the Serilog adapter "notification-aware" — it filters out channels that have a native adapter registered.
- **Option C:** Configuration-driven toggle: if `Notification:Telegram` config section exists, disable the Serilog Telegram sink at startup. Document this clearly.
- **Critical:** Add a test that verifies exactly one Telegram message is sent per notification event.

**Warning signs:**
- Users report duplicate Telegram messages after enabling the native adapter
- CI tests that count outbound HTTP calls show doubled counts for Telegram

**Phase to address:**
Phase 2 (Adapter registration / Serilog adapter) — the DI wiring must handle Serilog/native coexistence.

---

### Pitfall 3: Circular Dependency Between Port and ILogger

**What goes wrong:**
The `INotificationService` Serilog adapter depends on `ILogger` to forward notifications. But existing AppServices and jobs also use `ILogger` directly. If the adapter also logs its own failures through the same `ILogger`, and those failure logs match the Serilog notification filter, you get infinite error loops: notification fails → error logged → error hits notification sinks → notification fails again.

**Why it happens:**
The natural first implementation of `SerilogNotificationAdapter` is to inject `ILogger` and call `logger.LogInformation(notificationMessage)`. If the adapter also logs its own errors about failed deliveries through the same `ILogger`, and those errors flow through the same notification sinks, transient failures cascade.

**How to avoid:**
- The Serilog adapter should write to the Serilog pipeline via `Log.ForContext("Notification", true)` rather than injecting `ILogger<T>` — this lets you control which sinks see the message.
- For error handling: notification adapter delivery failures should be logged with a property that notification sinks **exclude**. Use `LogContext.PushProperty("NotificationOrigin", "adapter")` and configure notification sinks to filter `NotificationOrigin` != `adapter`.
- **Never** have notification adapter error logs flow through the notification sink pipeline.

**Warning signs:**
- Transient Telegram API failures cause log storms / CPU spikes
- Stack traces show recursive calls through notification → logger → notification
- Memory pressure from unbounded log buffers during API outages

**Phase to address:**
Phase 1 (Port design) and Phase 3 (Error handling) — the separation must be baked into the interface contract and adapter implementation.

---

### Pitfall 4: Over-Abstraction — Too Many Interfaces

**What goes wrong:**
The port layer grows to include `INotificationService`, `INotificationFormatter`, `INotificationChannel`, `INotificationChannelSelector`, `INotificationBatcher`, `INotificationRetryPolicy`, `INotificationRateLimiter`, etc. Each interface has one implementation. The abstraction cost exceeds the benefit, and new contributors can't trace a notification from call to delivery.

**Why it happens:**
Port/adapter pattern is seductive for "future-proofing." Each new concern (formatting, channel selection, batching, retry) gets its own interface because "what if we need to swap it later?" In practice, this system has exactly two adapter strategies (Serilog-based and native HTTP), and they share very little behavior.

**How to avoid:**
- Start with exactly **one interface**: `INotificationService` with `SendAsync(NotificationMessage, CancellationToken)` plus optional `FlushAsync(groupId, CancellationToken)`.
- The Serilog adapter is one class. The Telegram adapter is one class. Retry/rate-limiting belongs **inside** the Telegram adapter (not as separate abstractions) because Telegram's limits are specific to Telegram.
- Only extract a shared interface when a second native adapter (e.g., webhook) genuinely needs the same behavior.
- Rule: **YAGNI until you have three implementations.**

**Warning signs:**
- More than 3 interfaces in the notification port/adapter layer
- An interface has only one implementation
- New contributor asks "which interface does what?"

**Phase to address:**
Phase 1 (Port definition) — constrain the interface surface from the start.

---

### Pitfall 5: Configuration Drift Between Serilog and Port Config

**What goes wrong:**
Existing users configure Telegram notifications in `appsettings.json` under `Serilog:WriteTo:TelegramBatched` (bot token, chat ID). The new native Telegram adapter reads from a separate config section (e.g., `Notification:Telegram`). Users must now configure both — or configure only the new section and wonder why the old Serilog Telegram sink stopped working (because the native adapter disabled it per Pitfall 2). Worst case: they configure both sections with different values.

**Why it happens:**
Two independent config systems coexist with no migration path. The Serilog config is declarative (`WriteTo` blocks); the new config is a flat POCO section. There's no automatic derivation between them.

**How to avoid:**
- **Read from existing config first.** The native Telegram adapter should attempt to read `BotToken` and `ChatId` from the existing Serilog config path if the new `Notification:Telegram` section is absent. This gives zero-config migration.
- Provide the new `Notification:Telegram` section as the **override** path.
- Document: "If you have `Serilog:WriteTo:TelegramBatched` configured, the native adapter will use those values unless you explicitly set `Notification:Telegram`."
- Add a startup validation check: **warn** if both paths are configured with **different** values.

**Warning signs:**
- Users report "Telegram stopped working after upgrade" (config mismatch)
- GitHub issues asking "where do I put my bot token now?"
- Both config sections populated with different chat IDs

**Phase to address:**
Phase 2 (Configuration and migration) — config compatibility is a migration concern.

---

### Pitfall 6: Telegram API Rate Limiting Hits at Scale

**What goes wrong:**
The Telegram Bot API limits: ~1 message/second per individual chat, ~20 messages/minute per group, ~30 messages/second for bulk broadcasts (free tier). If the native Telegram adapter fires immediately (not batched) and a user has multiple accounts running concurrent jobs, burst rate can exceed 30/sec, triggering HTTP 429 responses with `retry_after` in the body.

**Why it happens:**
The existing Serilog batch model naturally rate-limits because all sinks flush once at job end. The native adapter, if it sends per-call, loses this natural rate-limiting. Concurrent jobs across 11 AppServices × multiple Bili accounts amplify the burst.

**How to avoid:**
- Implement a simple `SemaphoreSlim`-based rate limiter inside the Telegram adapter: max 25 messages/second (headroom below the 30/sec free limit).
- Respect `retry_after` from 429 responses: back off for the specified seconds, then retry.
- If the port uses batching (Pitfall 1 approach (a)), the adapter already fires once per job, so rate is naturally limited by job count, not message count.
- Log rate-limit hits at Warning level (not Error) to avoid triggering notification loops (Pitfall 3).

**Warning signs:**
- HTTP 429 responses in logs
- Telegram messages delayed by minutes
- Users report intermittent missing notifications (dropped after retry exhaustion)

**Phase to address:**
Phase 3 (Telegram adapter implementation) — rate limiting belongs in the adapter, not the port.

---

### Pitfall 7: Breaking ArchUnit Layer Direction Guardrails

**What goes wrong:**
The project has ArchUnitNET tests enforcing dependency direction across 5 layers: Agent, Application, DomainService, Infrastructure, Web. If the `INotificationService` port is placed in the wrong layer (e.g., Infrastructure instead of Application), or if the Telegram adapter references Web-layer types, architecture tests fail. Worse, if someone creates a new "Notification" layer project, it may not be covered by existing ArchUnit rules at all.

**Why it happens:**
The notification port sits at a boundary: called from Application layer (AppServices) but implemented in Infrastructure. The layer placement is not obvious. The existing project convention puts Agent interfaces in Application and implementations in Infrastructure — the notification port should follow the same pattern, but it's tempting to create a new project.

**How to avoid:**
- Follow the existing Agent pattern: `INotificationService` interface in `src/Ray.BiliBiliTool.Application/Ports/` (or equivalent Application-layer location), implementations in `src/Ray.BiliBiliTool.Infrastructure/`.
- Do **not** create a new top-level project for the notification boundary.
- Verify: ArchUnit tests still pass after adding the port. If a new assembly is introduced, add ArchUnit rules covering it.
- **Run `dotnet test` with architecture tests after every notification-related commit.**

**Warning signs:**
- ArchUnit tests fail after adding the port
- New `.csproj` created for notification
- `using` statements reference layers in the wrong direction

**Phase to address:**
Phase 1 (Port definition) — layer placement must be correct from the start.

---

### Pitfall 8: Losing the `fireInstanceId` Grouping Context

**What goes wrong:**
The existing system uses `LogContext.PushProperty(Constants.GroupPropertyKey, fireInstanceId)` in `BaseJob.Execute()` to group all log events from a single job run. The `BatchSinkManager` uses this property to flush the correct batch when the job ends. If the new `INotificationService` is called from a code path that doesn't have this `LogContext` property pushed (e.g., a direct API call, a background service, or a unit test), the Serilog adapter can't find the batch group, and the notification either gets lost or goes to the wrong group.

**Why it happens:**
The `LogContext` property is pushed in `BaseJob.Execute()`, which wraps `DoExecuteAsync()`. But if `INotificationService` is called from:
- A controller action (no job context)
- A background hosted service (no `fireInstanceId`)
- A unit test (no `LogContext`)

the batch routing breaks silently — messages are logged but never flushed because no `FlushAsync(fireInstanceId)` is called for that group.

**How to avoid:**
- The port's contract must document: "When called from a job context, messages are batched by `fireInstanceId` and flushed at job end. When called from non-job contexts, messages are sent immediately."
- The Serilog adapter should check for the presence of `Constants.GroupPropertyKey` in the current `LogContext`. If absent, log immediately (don't batch). This matches the existing Serilog behavior — sinks without batch grouping log in real time.
- Add tests for both paths: job-context (deferred flush) and non-job-context (immediate send).

**Warning signs:**
- Notifications from non-job code paths (API endpoints, background services) are silently lost
- Batch flush logs show empty groups for some code paths
- Tests pass when run in job context but fail in isolation

**Phase to address:**
Phase 1 (Port contract) and Phase 4 (Testing) — the dual-mode behavior must be designed and tested.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Wrap `ILogger` directly as `INotificationService` | Zero new infrastructure; just an adapter class | Blurs logging/notification boundary; every `ILogger.LogInformation` call becomes a potential notification; no control over notification-specific behavior | **Never** — the whole point of the port is to separate the concerns |
| Skip the Serilog adapter; only build native adapters | Simpler code; no Serilog interop | Breaks all 13 existing channels immediately; forces users to reconfigure everything at once | **Never** — violates the "zero breaking change" brownfield constraint |
| Use `IMediator`/MediatR for notifications | Familiar pattern; decouples via events | Adds a dependency not currently in the stack; conflates domain events with infrastructure notifications; harder to trace delivery path | Only if MediatR is already in the stack (it isn't) |
| Make `INotificationService` a self-registering `IServiceCollection.AddNotification<T>()` pattern | Flexible; each adapter self-registers | Over-engineering for 2 adapters; configuration discovery becomes implicit and hard to debug | When you have 4+ adapters with self-registration needs |
| Store Telegram config in SQLite alongside Bili account config | Centralized config store | Mixes infrastructure config (bot token) with domain data (user accounts); SQLite schema now owns deployment config | Only if config management is already in SQLite (it isn't for infrastructure) |
| Hard-code Telegram chat ID in adapter | Fast to implement | Single-user only; breaks multi-account setups; can't change without code change | **Never** — read from config |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Telegram Bot API | Sending messages immediately without rate limiting; getting HTTP 429 on concurrent job bursts | Rate-limit to 25 msg/sec; respect `retry_after` from 429 responses; batch messages per job |
| Telegram Bot API | Hard-coding `chat_id` in adapter; breaking multi-user notification | Read `chat_id` from config; support multiple chat IDs for multi-account setups |
| Telegram Bot API | Using MarkdownV2 parse mode without escaping special characters (`_`, `*`, `[`, `]`, `(`, `)`, `~`, `` ` ``, `>`, `#`, `+`, `-`, `=`, `\|`, `{`, `}`, `.`, `!`) | Escape all special chars, or use HTML parse mode; add a `SanitizeForTelegram(string)` helper |
| Telegram Bot API | Sending messages > 4096 characters (API rejects them) | Truncate and append "… (truncated)"; or split into multiple messages |
| Serilog sinks | Removing all Serilog notification sinks when adding native adapters | Remove only the specific sink replaced by a native adapter (e.g., only `TelegramBatched`); keep all others |
| Serilog LogContext | Assuming `fireInstanceId` is always present in `LogContext` | Check for the property; fall back to immediate send when absent |
| Existing `BatchSinkManager` | Calling `FlushAsync` from the new notification port | Never call `BatchSinkManager` from the port — `BaseJob` owns flush timing; the port only enqueues |
| appsettings.json | Adding new config sections without documenting relationship to existing Serilog config | Document precedence: `Notification:Telegram` overrides `Serilog:WriteTo:TelegramBatched`; warn on conflict |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Per-message HTTP calls in native Telegram adapter | High latency at job end if many notifications queued | Send a single summary message per job instead of per-event messages; or accept the cost and rate-limit | When a single job generates 10+ notification events (11 accounts × success + error) |
| Serilog adapter re-evaluates message templates on every call | CPU spike during high-throughput jobs | Pre-compile message templates; use `MessageTemplate` caching | When notification volume exceeds ~100 messages/job |
| Telegram adapter doesn't use `IHttpClientFactory` | Socket exhaustion under concurrent jobs | Use `IHttpClientFactory` — already the pattern for Refit clients in the project | When > 64 concurrent jobs (default `HttpClient` socket pool) |
| `BatchSinkManager.FlushAsync` called twice (once by `BaseJob`, once by notification port) | Duplicate flushes; sinks fire twice | The port must never call `BatchSinkManager` — only `BaseJob` does | When someone adds flush logic to the port interface |

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Port definition (INotificationService) | Interface shape doesn't accommodate batching semantics | Design with optional `FlushAsync` or document that batching is adapter-specific |
| Serilog adapter | Adapter becomes a thin `ILogger` wrapper with no value | Use `Log.ForContext("Notification", true)` so the adapter writes with a distinguishable marker |
| Telegram HTTP adapter | No rate limiting; no retry; no truncation; no parse mode escaping | Implement rate limiter, retry with backoff, 4096-char truncation, and parse-mode escaping |
| Configuration migration | Users with existing Serilog Telegram config get breakage or duplicates | Read from existing Serilog config as fallback; warn on conflict; document migration |
| DI registration | Both Serilog and native Telegram registered; duplicate delivery | Disable Serilog Telegram sink when native is configured; or Serilog adapter filters channels with native equivalents |
| Architecture tests | New notification assemblies break ArchUnit tests | Port in Application layer; adapter in Infrastructure; verify ArchUnit pass after each commit |
| Integration tests | Tests that verify notification delivery break because batch flush timing changed | Add characterization test: verify notification delivery timing matches pre-refactor behavior |

---

## Sources

- `src/Ray.BiliBiliTool.Web/Jobs/BaseJob.cs` — `BatchSinkManager.FlushAsync(fireInstanceId)` + `LogContext.PushProperty(Constants.GroupPropertyKey, fireInstanceId)` pattern
- `Directory.Packages.props` — 11 `Ray.Serilog.Sinks.*Batched` packages + Serilog core packages (versions 0.1.5 / 4.3.0 / 8.x)
- `.planning/PROJECT.md` — milestone context, constraints, 13 sink channels enumerated
- `.planning/phases/05-scheduler-shell-cleanup/05-01-PLAN.md` — Phase 5 preserved `BatchSinkManager flush` exactly
- Telegram Bot API official docs — rate limits: ~30 msg/sec bulk free, 1 msg/sec per chat, 20 msg/min per group; `retry_after` in 429 responses; 4096 char limit; MarkdownV2 escaping requirements
- `.planning/milestones/v4.0.0.1-MILESTONE-AUDIT.md` — ARCH-04 notification boundary gap documented
