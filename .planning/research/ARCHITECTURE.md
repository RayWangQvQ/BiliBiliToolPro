# Architecture Research — INotificationService Port/Adapter Boundary

**Domain:** .NET 8 port/adapter boundary for notification in a 5-layer brownfield app
**Researched:** 2026-05-07
**Confidence:** HIGH

> **Supersedes** the general refactor architecture research from prior milestone. This document is scoped specifically to the v4.0.0.8 notification boundary integration points.

---

## Current Architecture (Before Notification Boundary)
```
┌──────────────────────────────────────────────────────────────────────────┐
│  HOST LAYER  (Console / Web)                                            │
│  ┌────────────────────┐  ┌──────────────────────────────────────────┐   │
│  │ BiliBiliToolHosted │  │ BaseJob<T>                               │   │
│  │ Service            │  │  ┌─────────────────────────────────────┐ │   │
│  │  → DoTasksAsync()  │  │  │ DoExecuteAsync() → AppService      │ │   │
│  └────────┬───────────┘  │  │ catch → logger.LogError             │ │   │
│           │              │  │ finally → logger.LogInformation     │ │   │
│           │              │  └─────────────────────────────────────┘ │   │
│           │              │  finally → BatchSinkManager.FlushAsync() │   │
│           │              └──────────────────────┬───────────────────┘   │
├───────────┼──────────────────────────────────────┼──────────────────────┤
│  APPLICATION LAYER                              │                      │
│  ┌────────────────────┐  ┌─────────────────────┐│                      │
│  │ BaseMultiAccounts   │  │ TaskFlowDiagnostic  ││                      │
│  │ AppService          │  │ Scope               ││                      │
│  │  → DoTaskAccount    │  │  → logger.BeginScope││                      │
│  │    Async()          │  │  → FlowStart/End    ││                      │
│  └────────┬───────────┘  └─────────────────────┘│                      │
│           │                                      │                      │
│  ┌────────┴──────────────────────────────────────┴──────────────────┐   │
│  │ 11 Concrete AppServices (Daily, Login, Manga, Live, Charge...)  │   │
│  │  • ILogger<T> for diagnostic logging                            │   │
│  │  • TaskInterceptor attribute for step telemetry                 │   │
│  │  • TaskFlowDiagnosticScope wrapping each flow                   │   │
│  │  • NO notification abstraction — all output is ILogger calls    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────┤
│  DOMAIN SERVICE / DOMAIN / AGENT / INFRASTRUCTURE                       │
│  (unchanged by this work)                                               │
├─────────────────────────────────────────────────────────────────────────┤
│  SERILOG PIPELINE  (cross-cutting, configured in appsettings.json)      │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ Serilog LoggerConfiguration                                      │   │
│  │  → ReadFrom.Configuration (13 sinks via appsettings.json)        │   │
│  │  → Enrich.FromLogContext                                         │   │
│  │                                                                  │   │
│  │ Sinks: Console, File, Debug, Telegram, WorkWeChat×2, DingTalk,  │   │
│  │        ServerChan, CoolPush, OtherApi, PushPlus, Teams, Gotify,  │   │
│  │        SQLite (Web only)                                         │   │
│  └────────────────────────────┬─────────────────────────────────────┘   │
│                               │                                         │
│  ┌────────────────────────────┴─────────────────────────────────────┐   │
│  │ BatchSinkManager (Ray.Serilog.Sinks.Batched)                     │   │
│  │  • Accumulates log events per FireInstanceId (LogContext group)   │   │
│  │  • FlushAsync(fireInstanceId) called at job end                  │   │
│  │  • Pushes batched events to all configured batched sinks         │   │
│  └──────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities (Current)

| Component | Layer | Responsibility | How It Works |
|-----------|-------|----------------|--------------|
| `BaseJob<T>` | Web.Jobs | Quartz IJob entry; calls AppService then flushes | `DoExecuteAsync()` → catch/finally logging → `BatchSinkManager.FlushAsync(fireInstanceId)` |
| `BiliBiliToolHostedService` | Console | Console host entry; iterates tasks, calls AppService | `DoTasksAsync()` → resolves `IAppService` from DI, calls `DoTaskAsync()` |
| `BaseMultiAccountsAppService` | Application | Multi-account iteration with per-account error resilience | For each cookie: `DoTaskAccountAsync()`, catch-and-continue |
| Concrete AppServices | Application | Task-specific orchestration (Daily, Login, Manga, etc.) | `[TaskInterceptor]` + `TaskFlowDiagnosticScope` + domain service calls |
| `TaskFlowDiagnosticScope` | Application.Diagnostics | Structured log scope per task flow | `logger.BeginScope({FlowName, FlowId})` → `LogInformation("FlowStart")` |
| `TaskInterceptorAttribute` | Application.Attributes | Rougamo AOP aspect for step-level logging | Intercepts method calls, logs start/end/errors via ILogger |
| `BatchSinkManager` | Ray.Serilog.Sinks.Batched (NuGet) | Groups log events by FireInstanceId, flushes at job end | Reads `LogContext.PushProperty(GroupPropertyKey, fireInstanceId)`, batches until `FlushAsync()` |

### Current Notification Flow (All via ILogger)

```
AppService step completes
    │
    ├── logger.LogInformation("step result")     ← diagnostic log
    │       │
    │       └── Serilog pipeline (13 sinks)      ← sinks see ALL log events
    │           ├── Console sink                 ← always active
    │           ├── File sink                    ← always active
    │           ├── TelegramBatched sink         ← batched, needs FlushAsync
    │           ├── WorkWeiXinBatched sink       ← batched, needs FlushAsync
    │           ├── ...11 more batched sinks     ← all batched
    │           └── SQLite sink (Web only)       ← batched
    │
    └── TaskFlowDiagnosticScope log markers      ← FlowStart/FlowCompleted
            │
            └── Same Serilog pipeline

BaseJob.Execute() finally block
    │
    └── BatchSinkManager.FlushAsync(fireInstanceId)
            │
            └── All batched sinks emit their accumulated events
```

**Key observation:** There is no distinction between "diagnostic log" and "user-facing notification." Every `logger.LogInformation(...)` call in an AppService is simultaneously a log event AND a notification because the Telegram/WorkWeChat/etc. sinks forward everything above their `restrictedToMinimumLevel` threshold.

---

## Proposed Architecture (After Notification Boundary)

```
┌──────────────────────────────────────────────────────────────────────────┐
│  HOST LAYER  (Console / Web)                                            │
│  ┌────────────────────┐  ┌──────────────────────────────────────────┐   │
│  │ BiliBiliToolHosted │  │ BaseJob<T>                               │   │
│  │ Service            │  │  → DoExecuteAsync() → AppService         │   │
│  │  (unchanged)       │  │  → catch/finally logging (ILogger)       │   │
│  └────────────────────┘  │  → BatchSinkManager.FlushAsync()         │   │
│                          │    (unchanged — still flushes Serilog)    │   │
│                          └──────────────────────────────────────────┘   │
├──────────────────────────────────────────────────────────────────────────┤
│  APPLICATION LAYER                                                       │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ INotificationService (NEW — the PORT)                               ││
│  │                                                                     ││
│  │  Task SendAsync(NotificationMessage message,                       ││
│  │                  CancellationToken ct);                             ││
│  │  Task SendSummaryAsync(string title,                               ││
│  │                        IReadOnlyList<SummaryLine> lines,           ││
│  │                        CancellationToken ct);                      ││
│  └──────────────────────────────┬──────────────────────────────────────┘│
│                                 │                                       │
│  ┌──────────────────────────────┴──────────────────────────────────────┐│
│  │ NotificationMessage / SummaryLine (NEW — value objects)             ││
│  │                                                                     ││
│  │  record NotificationMessage(                                        ││
│  │      string Title,                                                  ││
│  │      string Body,                                                   ││
│  │      NotificationLevel Level,   // Info, Warning, Error             ││
│  │      string? GroupKey = null    // maps to FireInstanceId           ││
│  │  );                                                                 ││
│  │  record SummaryLine(string Label, string Value, string? Status);   ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ 11 Concrete AppServices (Daily, Login, Manga, Live, Charge...)     ││
│  │  • Continue to use ILogger<T> for diagnostic logging               ││
│  │  • NOW ALSO inject INotificationService for notification-worthy    ││
│  │    results (task summary, error alerts, success confirmations)      ││
│  └─────────────────────────────────────────────────────────────────────┘│
├──────────────────────────────────────────────────────────────────────────┤
│  DOMAIN SERVICE / DOMAIN / AGENT / INFRASTRUCTURE  (unchanged)          │
├──────────────────────────────────────────────────────────────────────────┤
│  INFRASTRUCTURE LAYER  (ADAPTERS — NEW)                                  │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ SerilogNotificationAdapter (DEFAULT adapter)                        ││
│  │                                                                     ││
│  │  • Implements INotificationService                                  ││
│  │  • Injects ILogger<SerilogNotificationAdapter>                     ││
│  │  • SendAsync → logger.LogInformation("[NOTIFICATION] {Title}: {Body}")│
│  │  • SendSummaryAsync → logger.LogInformation with formatted summary ││
│  │  • Log events flow through existing Serilog pipeline as before     ││
│  │  • BatchSinkManager.FlushAsync still works (same LogContext group) ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ TelegramHttpAdapter (NEW — native HTTP, proves extensibility)       ││
│  │                                                                     ││
│  │  • Implements INotificationService                                  ││
│  │  • Injects IHttpClientFactory (or Refit ITelegramBotApi)           ││
│  │  • Reads TelegramNotificationOptions from configuration            ││
│  │  • SendAsync → POST https://api.telegram.org/bot{token}/sendMessage││
│  │  • SendSummaryAsync → formats Markdown message, sends via HTTP     ││
│  │  • Does NOT go through Serilog at all                              ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ CompositeNotificationAdapter (fan-out to multiple adapters)         ││
│  │                                                                     ││
│  │  • Implements INotificationService                                  ││
│  │  • Injects IEnumerable<INotificationService> (other adapters)     ││
│  │  • SendAsync → fans out to all child adapters                      ││
│  │  • Errors in one adapter don't block others                        ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ Serilog Pipeline (UNCHANGED)                                        ││
│  │  • All 13 sinks continue to work via existing config               ││
│  │  • BatchSinkManager.FlushAsync still called in BaseJob             ││
│  │  • Diagnostic ILogger calls still flow through all sinks           ││
│  └─────────────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Port Placement: Where Does `INotificationService` Live?

**Decision: `Ray.BiliBiliTool.Application.Contracts`**

| Location Considered | Verdict | Reason |
|---------------------|---------|--------|
| `Application.Contracts` | ✅ **Chosen** | Follows existing pattern (`IAppService` lives here). AppServices reference this. Infrastructure can reference it. Hosts can wire DI. |
| `Application` | ❌ Rejected | Would force Infrastructure to reference Application, inverting the dependency direction ArchUnitNET enforces. |
| `DomainService` | ❌ Rejected | Notification is an application concern, not a domain policy. |
| New standalone project | ❌ Overkill | One interface + two DTOs doesn't justify a new .csproj. |

**File structure in `Application.Contracts`:**
```
Ray.BiliBiliTool.Application.Contracts/
├── IAppService.cs                          (existing)
├── IDailyTaskAppService.cs                 (existing)
├── ...
├── Notifications/                          (NEW folder)
│   ├── INotificationService.cs             (NEW — the port)
│   ├── NotificationMessage.cs              (NEW — value object)
│   ├── SummaryLine.cs                      (NEW — value object)
│   └── NotificationLevel.cs                (NEW — enum: Info, Warning, Error)
```

**Why Application.Contracts, not Application:**
- The existing ArchUnitNET rule "Application should not depend on web, scheduler, or transport DTO types" doesn't block Application.Contracts from defining ports.
- Infrastructure already references Application.Contracts (transitively through Application).
- This is the same pattern as `IAppService` — defined in Contracts, implemented elsewhere.

---

## Adapter Placement: Where Do Adapters Go?

**Decision: `Ray.BiliBiliTool.Infrastructure`**

| Location Considered | Verdict | Reason |
|---------------------|---------|--------|
| `Infrastructure` | ✅ **Chosen** | Follows existing pattern. Infrastructure already has `Cookie/`, `Enums/`, `Helpers/`. Notification adapters are infrastructure implementations of application ports. |
| `Infrastructure.EF` | ❌ Rejected | Notification adapters don't use EF. Infrastructure.EF is specifically for EF Core + SQLite concerns. |
| `Web/Services/` | ❌ Rejected | Would couple adapters to Web host; Console host needs them too. |
| New project | ❌ Overkill | Two adapter classes + one options class doesn't justify a new .csproj. |

**File structure in Infrastructure:**
```
Ray.BiliBiliTool.Infrastructure/
├── Cookie/                                 (existing)
├── Enums/                                  (existing)
├── Helpers/                                (existing)
├── Notifications/                          (NEW folder)
│   ├── SerilogNotificationAdapter.cs       (NEW — default adapter)
│   ├── TelegramHttpAdapter.cs              (NEW — native HTTP adapter)
│   ├── CompositeNotificationAdapter.cs     (NEW — fan-out)
│   └── Options/
│       └── TelegramNotificationOptions.cs  (NEW — config POCO)
```

**DI wiring location:** Infrastructure's existing `ServiceCollectionExtension` or a new `NotificationServiceCollectionExtension` that both hosts call.

---

## Integration Points — New vs. Modified Components

### New Components

| Component | Layer | What | Why |
|-----------|-------|------|-----|
| `INotificationService` | Application.Contracts | Port interface with `SendAsync` + `SendSummaryAsync` | Separates notification concern from diagnostic logging |
| `NotificationMessage` | Application.Contracts | Value object: title, body, level, groupKey | Structured notification payload |
| `SummaryLine` | Application.Contracts | Value object: label, value, status | Structured task result summary |
| `NotificationLevel` | Application.Contracts | Enum: Info, Warning, Error | Severity classification |
| `SerilogNotificationAdapter` | Infrastructure | Default INotificationService implementation via ILogger | Zero-breaking-change bridge to existing 13 sinks |
| `TelegramHttpAdapter` | Infrastructure | Native HTTP INotificationService implementation | Proves extensibility; independent of Serilog pipeline |
| `CompositeNotificationAdapter` | Infrastructure | Fan-out adapter | Allows multiple adapters to coexist |
| `TelegramNotificationOptions` | Infrastructure/Options | Config POCO for Telegram HTTP adapter | botToken, chatId, apiHost, proxy |

### Modified Components

| Component | Layer | Change | Risk |
|-----------|-------|--------|------|
| `BaseMultiAccountsAppService` | Application | Add `INotificationService?` optional constructor parameter; call `SendSummaryAsync` after all accounts complete | **Low** — additive; existing behavior preserved via null-check or default no-op |
| Concrete AppServices (11) | Application | Add `INotificationService?` constructor parameter; call `SendAsync` for key result points (not all log lines) | **Low** — each service gets one or two targeted `SendAsync` calls; all existing ILogger calls untouched |
| `DailyTaskAppService` | Application | Add summary notification after all 6 steps complete | **Low** — one new call at end of `DoTaskAccountAsync` |
| `LoginTaskAppService` | Application | Add success/failure notification | **Low** — one new call after cookie persistence |
| `BaseJob<T>` | Web.Jobs | **NO CHANGE** — `BatchSinkManager.FlushAsync` stays as-is | **None** — Serilog pipeline is independent of notification port |
| `BiliBiliToolHostedService` | Console | **NO CHANGE** — existing task loop untouched | **None** — notification is additive |
| `ServiceCollectionExtension` | Application.Extensions | **NO CHANGE** — Scanning for IAppService still works | **None** |
| Infrastructure DI registration | Infrastructure.Extensions | Add `AddNotificationServices()` extension method | **Low** — new method, called from both hosts |
| Web `Program.cs` | Web | Add `builder.Services.AddNotificationServices()` call | **Low** — one line addition |
| Console `Program.cs` | Console | Add `services.AddNotificationServices()` call | **Low** — one line addition |
| `appsettings.json` (both hosts) | Config | Add `TelegramNotification` section | **None** — new section, existing Serilog section untouched |
| ArchUnitNET tests | ArchitectureTests | Add rule: Application layer must not reference Infrastructure.Notifications directly | **Low** — additive test |

### Unchanged Components

| Component | Reason |
|-----------|--------|
| `BaseJob<T>.Execute()` | `BatchSinkManager.FlushAsync` still flushes Serilog pipeline independently |
| `TaskFlowDiagnosticScope` | Diagnostic logging stays on ILogger |
| `TaskInterceptorAttribute` | AOP logging stays on ILogger |
| All 13 Serilog sink packages | Continue to receive log events via Serilog pipeline |
| `BatchSinkManager` | Still groups by FireInstanceId, still flushed at job end |
| DomainService layer | No notification concern lives here |
| Agent layer | Unchanged |
| Infrastructure.EF | Unchanged |

---

## Data Flow Changes

### Before (All Notifications via ILogger → Serilog Sinks)

```
AppService step
    │
    └── logger.LogInformation("result")           ← single path
            │
            └── Serilog pipeline
                ├── Console sink
                ├── File sink
                ├── TelegramBatched sink          ← batched, user sees in Telegram
                ├── WorkWeiXinBatched sink        ← batched, user sees in WorkWeChat
                └── ...10 more sinks

Job end → BatchSinkManager.FlushAsync()           ← flushes all batched sinks
```

### After (Dual Path: ILogger for Diagnostics, INotificationService for User-Facing)

```
AppService step
    │
    ├── logger.LogInformation("step detail")      ← diagnostic log (unchanged)
    │       │
    │       └── Serilog pipeline                   ← ALL 13 sinks still see this
    │           ├── Console sink
    │           ├── File sink
    │           ├── TelegramBatched sink           ← still batched, still flushed
    │           └── ...10 more sinks
    │
    └── notificationService.SendAsync(msg)         ← NEW: notification port
            │
            ├── SerilogNotificationAdapter          ← default adapter
            │       │
            │       └── logger.LogInformation       ← enters Serilog pipeline
            │           "[NOTIFICATION] title: body"  (same as before, but tagged)
            │
            └── TelegramHttpAdapter                 ← NEW: native HTTP adapter
                    │
                    └── POST api.telegram.org        ← independent of Serilog
                        /bot{token}/sendMessage

Job end → BatchSinkManager.FlushAsync()            ← UNCHANGED, still works
```

### Key Insight: SerilogNotificationAdapter Reuses the Same Pipeline

The `SerilogNotificationAdapter` writes notification messages as `ILogger.LogInformation(...)` calls. These enter the same Serilog pipeline, hit the same 13 sinks, and are batched by the same `BatchSinkManager`. This means:

1. **Existing Telegram users still get notifications** — the `TelegramBatched` sink still receives log events
2. **BatchSinkManager.FlushAsync() still works** — it doesn't care where the log event came from
3. **LogContext group property still applies** — the FireInstanceId grouping is untouched

The **only new thing** is that `TelegramHttpAdapter` sends directly via HTTP, bypassing Serilog entirely. Users who configure BOTH the Serilog Telegram sink AND the native HTTP adapter would get duplicate Telegram messages — the configuration guidance must address this.

---

## How Serilog Adapter Routes Without Breaking BatchSinkManager

```csharp
// In Infrastructure/Notifications/SerilogNotificationAdapter.cs
public class SerilogNotificationAdapter : INotificationService
{
    private readonly ILogger<SerilogNotificationAdapter> _logger;

    public SerilogNotificationAdapter(ILogger<SerilogNotificationAdapter> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        // This log event enters the existing Serilog pipeline.
        // If TelegramBatched sink is configured, it will batch this event.
        // BatchSinkManager.FlushAsync(fireInstanceId) will flush it at job end.
        _logger.LogInformation("[Notification] {Title}: {Body}", message.Title, message.Body);
        return Task.CompletedTask;
    }

    public Task SendSummaryAsync(string title, IReadOnlyList<SummaryLine> lines,
                                  CancellationToken ct = default)
    {
        var body = string.Join(Environment.NewLine,
            lines.Select(l => $"  {l.Label}: {l.Value} [{l.Status}]"));
        _logger.LogInformation("[Summary] {Title}{NewLine}{Body}",
            title, Environment.NewLine, body);
        return Task.CompletedTask;
    }
}
```

**Why this doesn't break BatchSinkManager:**
- `BatchSinkManager` reads `LogContext.PushProperty(Constants.GroupPropertyKey, fireInstanceId)` — this is pushed in `BaseJob<T>.Execute()` before calling `DoExecuteAsync()`.
- Any `ILogger` call made during `DoExecuteAsync()` (including from `SerilogNotificationAdapter`) inherits this LogContext property.
- `BatchSinkManager.FlushAsync(fireInstanceId)` then flushes all events with that group key.
- The notification adapter's `logger.LogInformation(...)` call is just another log event in the same context — it gets batched and flushed exactly like all other log events.

---

## How TelegramHttpAdapter Coexists

```csharp
// In Infrastructure/Notifications/TelegramHttpAdapter.cs
public class TelegramHttpAdapter : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramNotificationOptions _options;

    public TelegramHttpAdapter(
        IHttpClientFactory httpClientFactory,
        IOptions<TelegramNotificationOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("TelegramBot");
        _options = options.Value;
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.BotToken) || string.IsNullOrEmpty(_options.ChatId))
            return; // not configured, skip silently

        var text = $"*{EscapeMarkdown(message.Title)}*\n{EscapeMarkdown(message.Body)}";

        var response = await _httpClient.PostAsJsonAsync(
            $"bot{_options.BotToken}/sendMessage",
            new { chat_id = _options.ChatId, text, parse_mode = "MarkdownV2" },
            ct);

        response.EnsureSuccessStatusCode();
    }

    public async Task SendSummaryAsync(string title, IReadOnlyList<SummaryLine> lines,
                                        CancellationToken ct = default)
    {
        // ... formats Markdown, POSTs to Telegram API
    }
}
```

**Coexistence rules:**
- `TelegramHttpAdapter` is independent of `SerilogNotificationAdapter`
- If both are registered, `CompositeNotificationAdapter` fans out to both
- Users should be guided: if you enable `TelegramHttpAdapter`, consider disabling `TelegramBatched` Serilog sink to avoid duplicate Telegram messages

---

## Changes to Existing AppServices and Jobs

### Jobs: NO CHANGES

| Job | Change | Why |
|-----|--------|-----|
| `BaseJob<T>` | None | `BatchSinkManager.FlushAsync()` still flushes Serilog; notification port is orthogonal |
| `DailyJob` | None | Delegates to `DailyTaskAppService` via `DoExecuteAsync()` |
| `LoginJob` | None | Delegates to `LoginTaskAppService` |
| All 12 other jobs | None | Thin delegation shells, unchanged |

### AppServices: Targeted Additions

Each AppService gets **one to two** `INotificationService.SendAsync/SendSummaryAsync` calls for notification-worthy outcomes. The existing `ILogger` calls remain untouched.

**Pattern for each AppService:**

```csharp
// Example: DailyTaskAppService (conceptual diff)
public class DailyTaskAppService(
    ILogger<DailyTaskAppService> logger,
    // ... existing params ...
    INotificationService? notificationService = null   // NEW: optional, backward-compatible
) : BaseMultiAccountsAppService(logger, cookieStrFactory, loginDomainService, configuration)
{
    [TaskInterceptor("每日任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(BiliCookie ck, CancellationToken ct)
    {
        await TaskFlowDiagnosticScope.ExecuteAsync(logger, "DailyTask", async () =>
        {
            // ... existing 6-step workflow (unchanged) ...

            // NEW: send summary notification after all steps
            if (notificationService != null)
            {
                await notificationService.SendSummaryAsync(
                    $"Daily Task Complete — {ck.UserId}",
                    [
                        new("Login", "OK", "✅"),
                        new("Watch Video", watchResult, watchResult == "completed" ? "✅" : "⚠️"),
                        new("Coins Donated", coinCount.ToString(), coinCount > 0 ? "✅" : "⚠️"),
                        new("VIP Privilege", vipResult, vipResult == "claimed" ? "✅" : "⚠️"),
                    ],
                    ct);
            }
        });
    }
}
```

**Why optional parameter (`= null`):**
- Backward compatible — existing DI registrations that don't register `INotificationService` won't break
- Progressive migration — add notification calls to one AppService at a time
- Test isolation — unit tests that don't set up notification don't fail

### Summary of AppService Changes

| AppService | Notification Points | Priority |
|------------|-------------------|----------|
| `DailyTaskAppService` | End-of-flow summary (6 steps) | Phase 1 |
| `LoginTaskAppService` | Login success/failure | Phase 1 |
| `ChargeTaskAppService` | Charge result | Phase 2 |
| `MangaTaskAppService` | Manga sign + read results | Phase 2 |
| `LiveLotteryTaskAppService` | Lottery results | Phase 2 |
| `LiveFansMedalAppService` | Medal progress | Phase 2 |
| `VipPrivilegeTaskAppService` | Privilege claim result | Phase 2 |
| `VipBigPointAppService` | Big point result | Phase 2 |
| `Silver2CoinTaskAppService` | Silver→coin conversion | Phase 3 |
| `MangaPrivilegeTaskAppService` | Manga privilege claim | Phase 3 |
| `UnfollowBatchedTaskAppService` | Unfollow batch result | Phase 3 |
| `TestAppService` | Test-only, no notification | Skip |

---

## DI Wiring

### Both Hosts Call a Shared Registration Method

```csharp
// In Infrastructure/Extensions/NotificationServiceCollectionExtension.cs
public static IServiceCollection AddNotificationServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Register options
    services.Configure<TelegramNotificationOptions>(
        configuration.GetSection("TelegramNotification"));

    // Register adapters
    services.AddSingleton<SerilogNotificationAdapter>();
    services.AddSingleton<TelegramHttpAdapter>();

    // Register composite as the INotificationService
    services.AddSingleton<INotificationService>(sp =>
    {
        var adapters = new List<INotificationService>
        {
            sp.GetRequiredService<SerilogNotificationAdapter>(),
        };

        var tgOptions = sp.GetRequiredService<IOptions<TelegramNotificationOptions>>().Value;
        if (!string.IsNullOrEmpty(tgOptions.BotToken))
        {
            adapters.Add(sp.GetRequiredService<TelegramHttpAdapter>());
        }

        return new CompositeNotificationAdapter(adapters);
    });

    return services;
}
```

### Web Host (Program.cs) — One Line Addition

```csharp
// After existing .AddCoreModuleServices(builder.Configuration):
builder.Services.AddNotificationServices(builder.Configuration);
```

### Console Host (Program.cs) — One Line Addition

```csharp
// In ConfigureServices or equivalent:
services.AddNotificationServices(configuration);
```

### ArchUnitNET Guardrail Update

```csharp
// New test in DependencyGuardrailTests.cs
[Fact]
public void Application_should_only_access_notification_port_through_contracts()
{
    // Verify Application code references INotificationService from Contracts,
    // not concrete adapters from Infrastructure
    // ... (add to existing test class)
}
```

---

## Suggested Build Order

### Phase 1: Port Definition (Application.Contracts)

**Dependencies:** None (pure interface + value objects)

1. Create `Application.Contracts/Notifications/NotificationLevel.cs` (enum)
2. Create `Application.Contracts/Notifications/NotificationMessage.cs` (record)
3. Create `Application.Contracts/Notifications/SummaryLine.cs` (record)
4. Create `Application.Contracts/Notifications/INotificationService.cs` (interface)
5. Build + existing tests pass (no consumers yet)

### Phase 2: Serilog Adapter (Infrastructure)

**Dependencies:** Phase 1 (port interface)

6. Create `Infrastructure/Notifications/SerilogNotificationAdapter.cs`
7. Create `Infrastructure/Notifications/CompositeNotificationAdapter.cs`
8. Create `Infrastructure/Extensions/NotificationServiceCollectionExtension.cs`
9. Add `AddNotificationServices()` call to Web `Program.cs`
10. Add `AddNotificationServices()` call to Console `Program.cs`
11. Build + all existing tests pass (no AppService changes yet — composite has no adapters registered, so it's a no-op)

### Phase 3: Telegram HTTP Adapter (Infrastructure)

**Dependencies:** Phase 2 (DI wiring in place)

12. Create `Infrastructure/Notifications/Options/TelegramNotificationOptions.cs`
13. Create `Infrastructure/Notifications/TelegramHttpAdapter.cs`
14. Add `TelegramNotification` section to `appsettings.json` (both hosts)
15. Update `NotificationServiceCollectionExtension` to conditionally register TelegramHttpAdapter
16. Build + existing tests pass

### Phase 4: Wire AppServices (Application) — DailyTask + LoginTask

**Dependencies:** Phase 2 (INotificationService registered in DI)

17. Add `INotificationService?` constructor parameter to `BaseMultiAccountsAppService`
18. Add `SendSummaryAsync` call to `DailyTaskAppService.DoTaskAccountAsync()`
19. Add `SendAsync` call to `LoginTaskAppService` for login success/failure
20. Architecture tests: verify Application doesn't reference Infrastructure directly
21. Characterization tests: verify existing behavior unchanged
22. Integration tests: verify notification calls made

### Phase 5: Wire Remaining AppServices

**Dependencies:** Phase 4 (pattern established)

23. Add `INotificationService?` + notification calls to remaining 9 AppServices (excluding TestAppService)
24. Architecture tests pass
25. All existing tests pass

### Phase 6: ArchUnitNET + Documentation

**Dependencies:** Phase 5 (all wiring complete)

26. Add ArchUnitNET guardrail for notification port boundary
27. Update README / configuration documentation
28. Configuration guidance: "If enabling TelegramHttpAdapter, consider disabling TelegramBatched sink"

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Duplicate Telegram messages (Serilog sink + HTTP adapter both active) | Medium | Config guidance; CompositeNotificationAdapter log warning if both active |
| Breaking existing DI resolution for AppServices | Low | `INotificationService?` optional parameter with `= null` default |
| BatchSinkManager loses events during transition | None | SerilogNotificationAdapter writes to same ILogger pipeline; LogContext unchanged |
| New adapter fails at runtime (Telegram API down) | Low | CompositeNotificationAdapter catches per-adapter exceptions; Serilog adapter still works |
| ArchUnitNET test fails because Application references Infrastructure | None | Application only references Application.Contracts; adapters are in Infrastructure |
| Console host doesn't have IHttpClientFactory registered | Low | Register in `NotificationServiceCollectionExtension.AddNotificationServices()` |

---

## Appendix: Prior General Architecture Research

> The following sections are preserved from the initial project research (v4.0.0.1 milestone). They describe the general modular monolith architecture direction. The notification boundary research above supersedes these sections for v4.0.0.8 scope.

### General Target Shape

Keep the product as a modular monolith with two thin composition roots: Web for HTTP, Blazor, and Quartz; Console for command-line or worker execution. Do not split into microservices and do not add a new framework layer.

Use feature modules as the main unit of change: Account, DailyTask, Manga, Live, Charge, Admin, and Scheduler orchestration. Each module should expose application use cases and internalize its implementation details.

### General Boundary Rules

- Hosts may depend on module registration and application contracts, but not directly on EF or Bilibili client details.
- Quartz jobs, controllers, and Razor components should call one application use case each.
- Application code may depend on Domain and on port interfaces, but not on concrete Agent, EF, or web types.
- Domain code must not depend on Web, Quartz, EF, HTTP clients, configuration providers, or UI DTOs.

### General Dependency Direction

`Web/Console/Quartz → Application → Domain ← Infrastructure/Agent implementations`

---

## Sources

- **Codebase analysis:** Direct inspection of `BaseJob.cs`, `BiliBiliToolHostedService.cs`, `BaseMultiAccountsAppService.cs`, `DailyTaskAppService.cs`, `DependencyGuardrailTests.cs`, both `appsettings.json` files, both `Program.cs` files
- **Existing pattern:** `IAppService` in `Application.Contracts` as the precedent for port placement
- **Existing pattern:** `IExecutionLogRepository` / `IUserRepository` in Infrastructure.EF as the precedent for adapter placement (v4.0.0.6 Web layer cleanup)
- **Existing pattern:** `ServiceCollectionExtension.AddAppServices()` scanning for port implementations
- **BatchSinkManager behavior:** Confirmed via `BaseJob<T>.Execute()` — `FlushAsync(fireInstanceId)` operates on LogContext group key, not on specific ILogger implementations
- **Confidence: HIGH** — All findings are from direct code inspection, not external documentation