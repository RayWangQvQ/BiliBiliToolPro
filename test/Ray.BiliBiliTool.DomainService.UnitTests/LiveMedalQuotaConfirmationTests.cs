using System.Text.Json;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.DomainService;
using Xunit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class LiveMedalQuotaConfirmationTests
{
    private readonly LiveFansMedalTaskTests.BudgetClock _clock = new()
    {
        Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.FromHours(8)),
    };
    private readonly BiliCookie _cookie = new(new() { ["DedeUserID"] = "1" });

    [Theory]
    [InlineData("like", 10)]
    [InlineData("sendDanmu", 1)]
    [InlineData("watchLive", 60)]
    public void SendingReservesCapacityWithoutConfirmingCompletion(string action, int amount)
    {
        var gate = new LiveFansMedalExecutionGate(_clock);
        Assert.Equal(amount, gate.Reserve("1", 60, action, amount, amount));
        Assert.Equal((0, amount), gate.Progress("1", 60, action));
        gate.Confirm("1", 60, action, amount / 2);
        Assert.Equal((amount / 2, amount - amount / 2), gate.Progress("1", 60, action));
        gate.Confirm("1", 60, action, amount, done: true);
        Assert.Equal((amount, 0), gate.Progress("1", 60, action));
    }

    [Fact]
    public void ExplicitRejectionDoesNotDebitConfirmedQuota()
    {
        var gate = new LiveFansMedalExecutionGate(_clock);
        gate.Confirm("1", 60, "like", 10);
        Assert.Equal(10, gate.ReserveInteraction(_cookie, 60, "like", 20, 10, out var date));
        gate.ReleaseRejectedInteraction(_cookie, 60, "like", 10, date);
        Assert.Equal((10, 0), gate.Progress("1", 60, "like"));
        Assert.Equal(10, gate.Remaining("1", 60, "like", 20));
        Assert.Equal(5000, gate.RemainingMonitoredLikes(_cookie));
    }

    [Fact]
    public void UnconfirmedRequestsCanBeRetriedAfterReconciliationWindowButKeepSendProtection()
    {
        var gate = new LiveFansMedalExecutionGate(_clock);
        using (gate.TryAcquire(_cookie, 60, "like"))
        {
            gate.ReserveInteraction(_cookie, 60, "like", 10, 10, out _);
            _clock.Now = _clock.Now.AddMinutes(6);
            Assert.Null(gate.TryAcquire(_cookie, 60, "like"));
            Assert.Equal((0, 10), gate.Progress("1", 60, "like"));
        }
        using var retry = gate.TryAcquire(_cookie, 60, "like");
        Assert.NotNull(retry);
        Assert.Equal((0, 0), gate.Progress("1", 60, "like"));
        Assert.Equal(4990, gate.RemainingMonitoredLikes(_cookie));
        // Fresh platform completion still prevents a retry after the reservation expires.
        gate.Confirm("1", 60, "like", 10, done: true);
        Assert.Equal(0, gate.Reserve("1", 60, "like", 10, 10));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestartAndLegacyMigrationDoNotTurnRequestsIntoCompletion(bool legacy)
    {
        var path = Path.Combine(Path.GetTempPath(), $"synthetic-quota-{Guid.NewGuid():N}.json");
        try
        {
            if (legacy)
                File.WriteAllText(
                    path,
                    JsonSerializer.Serialize(
                        new[]
                        {
                            new
                            {
                                User = "1",
                                Anchor = 60,
                                Action = "like",
                                Date = _clock.Now.Date,
                                Count = 10,
                            },
                        }
                    )
                );
            else
                new LiveFansMedalExecutionGate(_clock, path).ReserveInteraction(
                    _cookie,
                    60,
                    "like",
                    10,
                    10,
                    out _
                );
            var reopened = new LiveFansMedalExecutionGate(_clock, path);
            Assert.Equal((0, 10), reopened.Progress("1", 60, "like"));
            reopened.Confirm("1", 60, "like", 5);
            Assert.Equal(
                (5, 5),
                new LiveFansMedalExecutionGate(_clock, path).Progress("1", 60, "like")
            );
            _clock.Now = _clock.Now.AddMinutes(6);
            using var retry = reopened.TryAcquire(_cookie, 60, "like");
            Assert.Equal((5, 0), reopened.Progress("1", 60, "like"));
            Assert.Equal(
                4990,
                new LiveFansMedalExecutionGate(_clock, path).RemainingMonitoredLikes(_cookie)
            );
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MonitorEligibilityReleasesExpiredPendingCapacityAndReconcilesFreshProgress()
    {
        var gate = new LiveFansMedalExecutionGate(_clock);
        gate.ReserveInteraction(_cookie, 60, "like", 10, 10, out _);
        _clock.Now = _clock.Now.AddMinutes(6);
        // The monitor checks remaining capacity before acquiring a runner lease.
        Assert.Equal(5, gate.Remaining("1", 60, "like", 10, completed: 5));
        Assert.Equal((5, 0), gate.Progress("1", 60, "like"));
        Assert.Equal(4990, gate.RemainingMonitoredLikes(_cookie));
    }

    [Theory]
    [InlineData("sendDanmu", 100)]
    [InlineData("watchLive", 86400)]
    public void UnconfirmedRetryRetainsDailySendProtection(string action, int maximum)
    {
        var gate = new LiveFansMedalExecutionGate(_clock);
        gate.Reserve("1", 60, action, maximum, maximum);
        _clock.Now = _clock.Now.AddMinutes(6);
        using var retry = gate.TryAcquire(_cookie, 60, action);
        Assert.Equal((0, 0), gate.Progress("1", 60, action));
        Assert.Equal(0, gate.Reserve("1", 60, action, maximum, 1));
    }
}
