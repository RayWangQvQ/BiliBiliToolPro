# Technology Stack — Notification Port/Adapter Boundary

**Milestone:** v4.0.0.8 Notification Boundary
**Researched:** 2026-05-07
**Scope:** Only what's needed for `INotificationService` port/adapter + Telegram HTTP adapter

## Executive Summary

The notification boundary introduces **no new NuGet packages**. The entire port/adapter design is built on APIs already present in the .NET 8 SDK and the project's existing dependency set. The `INotificationService` port uses `Task`-based async dispatch. The Serilog adapter wraps the existing `ILogger` pipeline. The Telegram HTTP adapter uses the already-referenced `IHttpClientFactory` (`Microsoft.Extensions.Http` v8.0.1) and `System.Text.Json`. Configuration binding uses the existing `IOptionsMonitor<T>` + `Configuration.GetSection()` pattern already established in `ServiceCollectionExtension.AddBiliBiliConfigs()`.

## Stack Decision Table

| Concern | Technology | Version | Already Present? | Where |
|---------|-----------|---------|------------------|-------|
| Port interface | `INotificationService` (project-owned) | N/A | No — create in `Application.Contracts` | `Ray.BiliBiliTool.Application.Contracts` |
| Async dispatch | `Task` / `Task<T>` (.NET BCL) | net8.0 | Yes | BCL |
| DI registration | `IServiceCollection` + `Add*` extension | 8.0.2 | Yes | All layers |
| Configuration binding | `IOptionsMonitor<T>` + `GetSection()` | 8.0.2 | Yes | `Ray.BiliBiliTool.Config` |
| HTTP client factory | `Microsoft.Extensions.Http` | 8.0.1 | Yes | `Directory.Packages.props` line 13 |
| HTTP client for Telegram | `HttpClient` via `IHttpClientFactory` | 8.0.1 | Yes (same package) | `Microsoft.Extensions.Http` |
| JSON serialization | `System.Text.Json` (BCL) | net8.0 | Yes | BCL |
| Logging (adapter passthrough) | `ILogger<T>` / `ILogger` | 8.0.3 | Yes | `Microsoft.Extensions.Logging.Abstractions` |
| Test assertions | `FluentAssertions` | 8.5.0 | Yes | `Directory.Packages.props` line 82 |
| Test doubles | `NSubstitute` | latest | **Add** | Test projects only |

## Why No New Runtime Packages

### HttpClient — Already Enough

The Telegram Bot API is a simple REST API over HTTPS:
- Endpoint: `https://api.telegram.org/bot{token}/sendMessage`
- Method: POST with JSON body (`{"chat_id": "...", "text": "...", "parse_mode": "HTML"}`)
- Response: JSON with `ok: true/false` and `result` field

`HttpClient` from `Microsoft.Extensions.Http` handles this natively. No Telegram SDK or bot framework is needed. The existing project already uses `IHttpClientFactory` for all 18 Refit-based Bilibili clients — the same pattern applies.

**Why not a Telegram NuGet library (Telegram.Bot, etc.):**
- Adds ~20 transitive dependencies for a feature that needs one HTTP POST
- The existing `Ray.Serilog.Sinks.TelegramBatched` already has its own `TelegramApiClient` — adding another library creates two Telegram client codepaths
- The milestone goal is to prove the *port/adapter pattern works*, not to build a feature-rich Telegram integration
- Future adapters (WorkWeChat, DingTalk, etc.) will also be simple HTTP POSTs — the same pattern scales

### System.Text.Json — BCL Is Enough

The Telegram API request/response is trivial JSON. `System.Text.Json` with `JsonSerializer.Serialize()` and `JsonSerializer.Deserialize<T>()` handles it without `Newtonsoft.Json`.

### IOptionsMonitor<T> — Already the Convention

The project already binds configuration sections via `IOptionsMonitor<T>`:
```csharp
// Existing pattern in ServiceCollectionExtension.AddBiliBiliConfigs():
.Configure<SecurityOptions>(configuration.GetSection("Security"))
.Configure<DailyTaskOptions>(configuration.GetSection("DailyTaskConfig"))
```

The Telegram adapter follows the same pattern:
```csharp
.Configure<TelegramNotificationOptions>(configuration.GetSection("Notification:Telegram"))
```

## What to Add — Test Only

| Package | Version | Purpose | Where |
|---------|---------|---------|-------|
| `NSubstitute` | 5.3.0 | Mock `INotificationService` in unit tests | Test `.csproj` files only |

**Why NSubstitute over Moq:** The codebase already uses xUnit + FluentAssertions. NSubstitute is lighter, has no expression-tree overhead, and pairs well with FluentAssertions' `.Should().HaveReceived()` syntax. Moq would work too — this is a style recommendation, not a hard constraint.

**Version note:** Check NuGet.org for the latest NSubstitute stable release at implementation time. The existing `Directory.Packages.props` uses centrally managed versions — add the entry there.

## Layer Placement

```
Application.Contracts/          ← Port: INotificationService interface
    INotificationService.cs
    NotificationMessage.cs

Application/                    ← Serilog adapter: routes through ILogger
    Notifications/
        SerilogNotificationService.cs

Infrastructure/                 ← Telegram HTTP adapter: HttpClient POST
    Notifications/
        TelegramNotificationService.cs
        TelegramNotificationOptions.cs

Config/                         ← Options binding registration
    Options/
        TelegramNotificationOptions.cs
    Extensions/
        ServiceCollectionExtension.cs  (add .Configure<...> call)
```

### Why This Placement

| Component | Layer | Rationale |
|-----------|-------|-----------|
| `INotificationService` | Application.Contracts | Port must be visible to all layers; matches existing `IAppService` convention |
| `NotificationMessage` | Application.Contracts | DTO for the port — same layer as other request/response DTOs |
| `SerilogNotificationService` | Application | Wraps `ILogger` — Application already depends on `Microsoft.Extensions.Logging` |
| `TelegramNotificationOptions` | Config | Matches existing options pattern (`SecurityOptions`, `DailyTaskOptions`, etc.) |
| `TelegramNotificationService` | Infrastructure | Uses `IHttpClientFactory` — Infrastructure already depends on the right packages |

### Dependency Direction

```
Web/Console hosts → Application.Contracts (INotificationService)
                  → Application (SerilogNotificationService — default)
                  → Infrastructure (TelegramNotificationService — optional)
                  → Config (options binding)

Application.Contracts has NO dependencies on implementation layers.
Application depends on Application.Contracts + DomainService + Agent (existing).
Infrastructure depends on Domain (existing).
```

This respects the ArchUnitNET guardrails already enforcing layer direction.

## HttpClient Registration Pattern

Follow the existing project convention for named/typed clients:

```csharp
// In Telegram DI registration extension method:
services.AddHttpClient("TelegramBot", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptionsMonitor<TelegramNotificationOptions>>().CurrentValue;
    client.BaseAddress = new Uri(options.ApiHost ?? "https://api.telegram.org");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Why named client, not typed client:** The Telegram adapter is a single-class concern, not a Refit interface. A named client avoids creating a Refit interface for one endpoint. The existing `AddRefitClient<T>()` pattern handles the Bilibili APIs — this is a different, simpler concern.

**Proxy support:** The existing TelegramSerilog sink supports `proxy: "user:password@host:port"`. The new adapter should accept the same config key. Implement via `HttpClientHandler.Proxy` or `IWebProxy` injected into the `HttpClient` configuration:

```csharp
services.AddHttpClient("TelegramBot", (sp, client) => { ... })
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var options = sp.GetRequiredService<IOptionsMonitor<TelegramNotificationOptions>>().CurrentValue;
        var handler = new HttpClientHandler();
        if (!string.IsNullOrEmpty(options.Proxy))
        {
            handler.Proxy = ParseProxy(options.Proxy);
            handler.UseProxy = true;
        }
        return handler;
    });
```

## Configuration Shape

```json
{
  "Notification": {
    "Telegram": {
      "IsEnable": true,
      "BotToken": "",
      "ChatId": "",
      "ParseMode": "HTML",
      "ApiHost": "https://api.telegram.org",
      "Proxy": ""
    }
  }
}
```

**Why separate from Serilog section:** The Serilog config (`Serilog:WriteTo:3:Args:botToken`) controls the *logging sink*. The new section controls the *application-level notification adapter*. They may have the same values, but they serve different purposes and will diverge when the Serilog sinks are eventually deprecated.

## What NOT to Add

| Anti-Add | Why |
|----------|-----|
| `Telegram.Bot` NuGet | Overkill for one HTTP POST; adds 20+ transitive deps |
| `Newtonsoft.Json` | `System.Text.Json` handles Telegram JSON natively |
| `MediatR` / mediator pattern | One interface + two implementations doesn't need indirection |
| `Polly` (new) | `Microsoft.Extensions.Http.Polly` v8.0.18 already in `Directory.Packages.props`; use if retry is needed for Telegram POST, but don't add it — it's already there |
| New Serilog sink package | The whole point is to move *off* Serilog for notifications |
| `Microsoft.Extensions.Http.Resilience` | Not needed for the Telegram adapter scope; the existing Polly setup suffices if retry is wanted |

## Integration Points with Existing Infrastructure

### BatchSinkManager Coexistence

The new `INotificationService` runs alongside (not replacing) the existing Serilog batch flush:

1. **During transition:** Both systems send notifications. `BaseJob.Execute()` calls `BatchSinkManager.FlushAsync()` (existing) AND `INotificationService.SendBatchAsync()` (new).
2. **After migration:** Application code calls `INotificationService` directly for task summaries. `BatchSinkManager` continues handling log-based sinks until each sink is individually migrated.
3. **No breaking change:** Serilog sink configuration in `appsettings.json` remains untouched. Users who only use Serilog sinks see zero difference.

### ILogger Coexistence

`ILogger<T>` stays for diagnostic logging (debug, trace, error). `INotificationService` is for user-facing notifications (task results, summaries, alerts). The distinction:

| Concern | Abstraction | Example |
|---------|-------------|---------|
| "Daily task completed, 5 videos watched" | `INotificationService` | User notification |
| "Calling API endpoint X with params Y" | `ILogger` | Diagnostic log |
| "Coin balance check returned 342" | `ILogger` (Information) | Diagnostic log |
| "⚠️ Login failed for account 123" | `INotificationService` | User alert |

### Host Integration

**Console host** (`BiliBiliToolHostedService.DoTasksAsync`):
```csharp
// After task execution, send summary notification
await notificationService.SendBatchAsync(cancellationToken);
```

**Web host** (`BaseJob.DoExecuteAsync`):
```csharp
// After job execution, send summary notification
await notificationService.SendBatchAsync(cancellationToken);
```

The notification dispatch happens at the same lifecycle point as `BatchSinkManager.FlushAsync()` — in the `finally` block after task completion.

## Confidence Assessment

| Decision | Confidence | Source |
|----------|------------|--------|
| No new NuGet packages needed | HIGH | Verified against `Directory.Packages.props` and existing csproj files |
| HttpClient sufficient for Telegram | HIGH | Telegram Bot API docs (HTTP POST to `/bot{token}/sendMessage`) |
| Layer placement matches conventions | HIGH | Observed from existing code structure + ArchUnitNET rules |
| IOptionsMonitor pattern | HIGH | Existing `ServiceCollectionExtension.AddBiliBiliConfigs()` |
| NSubstitute version | MEDIUM | Must verify latest stable at implementation time |
| Proxy handling pattern | MEDIUM | Based on existing `TelegramApiClient` test usage; verify exact proxy format |

## Sources

- Telegram Bot API: https://core.telegram.org/bots/api#sendmessage (accessed 2026-05-07)
- `Directory.Packages.props` — central NuGet version management (project file)
- `src/Ray.BiliBiliTool.Config/Extensions/ServiceCollectionExtension.cs` — options binding pattern
- `src/Ray.BiliBiliTool.Agent/Extensions/ServiceCollectionExtension.cs` — HttpClient + Refit registration
- `src/Ray.BiliBiliTool.Web/Jobs/BaseJob.cs` — BatchSinkManager.FlushAsync() call site
- `test/LogTest/TestTelegram.cs` — existing TelegramApiClient usage from Serilog sink package
- `.planning/codebase/CONVENTIONS.md` — DI, HTTP client, and configuration conventions