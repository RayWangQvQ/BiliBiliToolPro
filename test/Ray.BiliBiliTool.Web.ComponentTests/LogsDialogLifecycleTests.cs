using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Components.Pages.Schedules;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;
using Xunit;
using Key = BlazingQuartz.Core.Models.Key;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class LogsDialogLifecycleTests : TestContext
{
    private IRenderedComponent<MudDialogProvider>? _provider;
    private readonly Workflow _workflow = new();
    private readonly Clock _clock = new();
    private readonly LogCapture _logger = new();

    public LogsDialogLifecycleTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<ILogsDialogWorkflow>(_workflow);
        Services.AddSingleton<TimeProvider>(_clock);
        Services.AddSingleton<ILogger<LogsDialog>>(_logger);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void FrameworkDisposalContractAndRepeatedDispose_AreSupported()
    {
        var component = new LogsDialog();
        try
        {
            Assert.IsAssignableFrom<IDisposable>(component);
            component.Dispose();
            component.Dispose();
        }
        finally
        {
            try
            {
                component.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
    }

    [Fact]
    public async Task ClosingDialog_CancelsQueriesAndStopsTimer()
    {
        var opened = await Open();
        try
        {
            var token = _workflow.Tokens.First();
            await opened.Provider.InvokeAsync(() => opened.Dialog.Close());
            opened.Provider.WaitForAssertion(() =>
                Assert.Empty(opened.Provider.FindComponents<LogsDialog>())
            );
            Assert.True(token.IsCancellationRequested);
            Assert.True(Assert.Single(_clock.Timers).Disposed);
            var calls = _workflow.Reads;
            _clock.Timers[0].Fire();
            await opened.Provider.InvokeAsync(() => Task.CompletedTask);
            Assert.Equal(calls, _workflow.Reads);
        }
        finally
        {
            Cleanup(opened.Component);
        }
    }

    [Fact]
    public async Task DisposeDuringLatestLookup_DoesNotStartLateQueryOrTimer()
    {
        var pending = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _workflow.Lookup = (_, _) => pending.Task;
        var component = CreateDetached();
        var initialization = Initialize(component);
        component.Dispose();
        pending.SetResult("synthetic-run");
        try
        {
            await initialization;
            Assert.Equal(0, _workflow.Reads);
            Assert.Empty(_clock.Timers);
        }
        finally
        {
            Cleanup(component);
        }
    }

    [Fact]
    public async Task Closing_CancelsPendingLookupWithoutWaitingForItsResult()
    {
        var pending = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _workflow.Lookup = (_, _) => pending.Task;
        var component = CreateDetached();
        var initialization = Initialize(component);
        component.Dispose();
        try
        {
            await initialization.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(pending.Task.IsCompleted);
            Assert.Equal(0, _workflow.Reads);
        }
        finally
        {
            pending.TrySetResult(null);
            Cleanup(component);
        }
    }

    [Fact]
    public async Task QueryIgnoringCancellation_CannotUpdateDisposedComponent()
    {
        var pending = new TaskCompletionSource<List<BiliLogs>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _workflow.Read = (_, _) => pending.Task;
        var component = CreateDetached();
        var initialization = Initialize(component);
        Assert.Single(_workflow.Tokens);
        component.Dispose();
        pending.SetResult(Logs("late-result"));
        try
        {
            await initialization;
            Assert.Empty(Get<List<BiliLogs>>(component, "_logs"));
            Assert.Empty(_clock.Timers);
        }
        finally
        {
            Cleanup(component);
        }
    }

    [Fact]
    public async Task AutomaticRefresh_UsesThreeSecondsWithoutDuplicateInitialRead()
    {
        var opened = await Open();
        try
        {
            var timer = Assert.Single(_clock.Timers);
            Assert.Equal(TimeSpan.FromSeconds(3), timer.DueTime);
            Assert.Equal(TimeSpan.FromSeconds(3), timer.Period);
            Assert.Equal(1, _workflow.Reads);
            _workflow.Read = (_, _) => Task.FromResult(Logs("updated"));
            timer.Fire();
            opened.Provider.WaitForAssertion(() =>
                Assert.Contains("updated", opened.Provider.Markup)
            );
            Assert.Equal(2, _workflow.Reads);
        }
        finally
        {
            Cleanup(opened.Component);
        }
    }

    [Fact]
    public async Task SlowRefresh_DoesNotOverlapManualOrAutomaticRefresh()
    {
        var opened = await Open();
        var pending = new TaskCompletionSource<List<BiliLogs>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        try
        {
            _workflow.Read = (_, token) => pending.Task.WaitAsync(token);
            var first = opened.Provider.InvokeAsync(() => Refresh(opened.Component));
            opened.Provider.WaitForAssertion(() => Assert.True(_workflow.Active > 0));
            var second = opened.Provider.InvokeAsync(() => Refresh(opened.Component));
            Assert.True(second.IsCompleted);
            Assert.Equal(1, _workflow.Active);
            foreach (var timer in _clock.Timers)
                timer.Fire();
            await opened.Provider.InvokeAsync(() => Task.CompletedTask);
            Assert.Equal(2, _workflow.Reads);
            pending.SetResult(Logs("after-slow-query"));
            await first;
            await second;
            Assert.Equal(1, _workflow.PeakActive);
        }
        finally
        {
            pending.TrySetResult(Logs("cleanup"));
            Cleanup(opened.Component);
        }
    }

    [Fact]
    public async Task RefreshFailure_RetainsPreviousLogsAndAllowsRetry()
    {
        var opened = await Open();
        try
        {
            _workflow.Read = (_, _) => throw new IOException("synthetic-private-detail");
            await opened.Provider.InvokeAsync(() => Refresh(opened.Component));
            Assert.Contains("initial", opened.Provider.Markup);
            Assert.False(Get<bool>(opened.Component, "_loading"));
            Assert.DoesNotContain(
                _logger.Messages,
                message => message.Contains("synthetic-private-detail")
            );
            _workflow.Read = (_, _) => Task.FromResult(Logs("retry-result"));
            await opened.Provider.InvokeAsync(() => Refresh(opened.Component));
            Assert.Contains("retry-result", opened.Provider.Markup);
        }
        finally
        {
            Cleanup(opened.Component);
        }
    }

    [Fact]
    public async Task MissingTrigger_PreservesNullableLookupScope()
    {
        var opened = await Open(nullTrigger: true);
        try
        {
            Assert.Null(_workflow.TriggerName);
            Assert.Equal("synthetic-job", _workflow.JobName);
            Assert.Contains("initial", opened.Provider.Markup);
        }
        finally
        {
            Cleanup(opened.Component);
        }
    }

    [Fact]
    public async Task NoLatestRun_DoesNotKeepLoadingOrReadNullRun()
    {
        _workflow.Lookup = (_, _) => Task.FromResult<string?>(null);
        var opened = await Open(expectLogs: false);
        try
        {
            Assert.False(Get<bool>(opened.Component, "_loading"));
            Assert.Empty(_clock.Timers);
            await opened.Provider.InvokeAsync(() => Refresh(opened.Component));
            Assert.Equal(0, _workflow.Reads);
        }
        finally
        {
            Cleanup(opened.Component);
        }
    }

    [Fact]
    public async Task FailedLookup_DoesNotBreakDialogOrStartTimer()
    {
        _workflow.Lookup = (_, _) => throw new IOException("synthetic-private-detail");
        var opened = await Open(expectLogs: false);
        try
        {
            Assert.False(Get<bool>(opened.Component, "_loading"));
            Assert.Equal(0, _workflow.Reads);
            Assert.Empty(_clock.Timers);
            Assert.DoesNotContain("synthetic-private-detail", opened.Provider.Markup);
            Assert.DoesNotContain(
                _logger.Messages,
                message => message.Contains("synthetic-private-detail")
            );
        }
        finally
        {
            Cleanup(opened.Component);
        }
    }

    [Fact]
    public async Task ClearedDisplay_CanReceiveNextAutomaticRefresh()
    {
        var opened = await Open();
        try
        {
            opened.Provider.FindAll("button.clear-button")[1].Click();
            Assert.Empty(Get<List<BiliLogs>>(opened.Component, "_logs"));
            _workflow.Read = (_, _) => Task.FromResult(Logs("new-entry"));
            Assert.Single(_clock.Timers).Fire();
            opened.Provider.WaitForAssertion(() =>
                Assert.Contains("new-entry", opened.Provider.Markup)
            );
        }
        finally
        {
            Cleanup(opened.Component);
        }
    }

    [Fact]
    public async Task ReopeningDialog_UsesIndependentTimerAndCancellation()
    {
        var first = await Open();
        try
        {
            var firstToken = _workflow.Tokens.First();
            await first.Provider.InvokeAsync(() => first.Dialog.Close());
            first.Provider.WaitForAssertion(() =>
                Assert.Empty(first.Provider.FindComponents<LogsDialog>())
            );
            Assert.True(firstToken.IsCancellationRequested);
            var second = await Open();
            try
            {
                Assert.Equal(2, _clock.Timers.Count);
                Assert.True(_clock.Timers[0].Disposed);
                Assert.False(_clock.Timers[1].Disposed);
                Assert.False(_workflow.Tokens.Last().IsCancellationRequested);
                Assert.NotEqual(firstToken, _workflow.Tokens.Last());
                await second.Provider.InvokeAsync(() => second.Dialog.Close());
                second.Provider.WaitForAssertion(() =>
                    Assert.Empty(second.Provider.FindComponents<LogsDialog>())
                );
                Assert.True(_clock.Timers[1].Disposed);
            }
            finally
            {
                Cleanup(second.Component);
            }
        }
        finally
        {
            Cleanup(first.Component);
        }
    }

    private async Task<Opened> Open(bool nullTrigger = false, bool expectLogs = true)
    {
        var provider = _provider ??= RenderComponent<MudDialogProvider>();
        var service = ((IServiceProvider)Services).GetRequiredService<IDialogService>();
        var dialog = await provider.InvokeAsync(() =>
            service.ShowAsync<LogsDialog>(
                "日志",
                new DialogParameters<LogsDialog>
                {
                    { component => component.JobKey, new Key("synthetic-job") },
                    {
                        component => component.TriggerKey,
                        nullTrigger ? null : new Key("synthetic-trigger")
                    },
                }
            )
        );
        provider.WaitForAssertion(() => Assert.Single(provider.FindComponents<LogsDialog>()));
        var component = provider.FindComponent<LogsDialog>().Instance;
        if (expectLogs)
            provider.WaitForAssertion(() => Assert.Contains("initial", provider.Markup));
        return new(provider, dialog, component);
    }

    private LogsDialog CreateDetached()
    {
        var component = new LogsDialog
        {
            JobKey = new Key("synthetic-job"),
            TriggerKey = new Key("synthetic-trigger"),
        };
        typeof(LogsDialog)
            .GetProperty("LogsWorkflow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(component, _workflow);
        typeof(LogsDialog)
            .GetProperty("Clock", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(component, _clock);
        typeof(LogsDialog)
            .GetProperty("Logger", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(component, _logger);
        return component;
    }

    private static Task Initialize(LogsDialog component) =>
        (Task)
            typeof(LogsDialog)
                .GetMethod("OnInitializedAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(component, null)!;

    private static Task Refresh(LogsDialog component) =>
        (Task)
            typeof(LogsDialog)
                .GetMethod("OnRefreshLogs", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(component, null)!;

    private static T Get<T>(LogsDialog component, string field) =>
        (T)
            typeof(LogsDialog)
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(component)!;

    private static void Cleanup(LogsDialog component)
    {
        try
        {
            component.Dispose();
        }
        catch (ObjectDisposedException) { }
    }

    private sealed record Opened(
        IRenderedComponent<MudDialogProvider> Provider,
        IDialogReference Dialog,
        LogsDialog Component
    );

    private static List<BiliLogs> Logs(string message) =>
        [
            new()
            {
                Timestamp = new DateTime(2026, 1, 1),
                Level = "information",
                RenderedMessage = message,
            },
        ];

    private sealed class Workflow : ILogsDialogWorkflow
    {
        public Func<string, string?, Task<string?>> Lookup = (_, _) =>
            Task.FromResult<string?>("synthetic-run");
        public Func<string, CancellationToken, Task<List<BiliLogs>>> Read = (_, _) =>
            Task.FromResult(Logs("initial"));
        public List<CancellationToken> Tokens { get; } = [];
        public int Reads,
            Active,
            PeakActive;
        public string? JobName,
            TriggerName;

        public Task<string?> GetLatestRunInstanceIdAsync(string jobName, string? triggerName)
        {
            JobName = jobName;
            TriggerName = triggerName;
            return Lookup(jobName, triggerName);
        }

        public async Task<List<BiliLogs>> GetLogsForRunAsync(
            string fireInstanceId,
            int maxCount,
            CancellationToken token
        )
        {
            Assert.Equal("synthetic-run", fireInstanceId);
            Assert.Equal(300, maxCount);
            Reads++;
            Tokens.Add(token);
            Active++;
            PeakActive = Math.Max(PeakActive, Active);
            try
            {
                return await Read(fireInstanceId, token);
            }
            finally
            {
                Active--;
            }
        }
    }

    private sealed class Clock : TimeProvider
    {
        public List<ClockTimer> Timers { get; } = [];

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period
        )
        {
            var timer = new ClockTimer(callback, state, dueTime, period);
            Timers.Add(timer);
            return timer;
        }
    }

    private sealed class ClockTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period
    ) : ITimer
    {
        public TimeSpan DueTime { get; private set; } = dueTime;
        public TimeSpan Period { get; private set; } = period;
        public bool Disposed { get; private set; }

        public bool Change(TimeSpan due, TimeSpan repeat)
        {
            DueTime = due;
            Period = repeat;
            return !Disposed;
        }

        public void Fire()
        {
            if (!Disposed)
                callback(state);
        }

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class LogCapture : ILogger<LogsDialog>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel level) => true;

        public void Log<TState>(
            LogLevel level,
            EventId id,
            TState state,
            Exception? error,
            Func<TState, Exception?, string> formatter
        ) => Messages.Add(formatter(state, error));
    }
}
