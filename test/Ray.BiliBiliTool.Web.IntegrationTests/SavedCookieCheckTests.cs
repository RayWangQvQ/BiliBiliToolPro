using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;
using Xunit;

namespace Ray.BiliBiliTool.Web.IntegrationTests;

public class SavedCookieCheckTests : IDisposable
{
    private const string Cookie = "DedeUserID=123;SESSDATA=synthetic;bili_jct=synthetic";
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory(
        "saved-cookie-check-"
    );
    private readonly IConfigurationRoot _configuration;

    public SavedCookieCheckTests() =>
        _configuration = new ConfigurationBuilder()
            .AddSqlite($"Data Source={Path.Combine(_directory.FullName, "settings.db")}")
            .Build();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QrLoginCompleteAsync_SavedLoginIsCheckedImmediatelyAndSurvivesCheckFailure(
        bool checkFails
    )
    {
        var guard = new CaptureGuard(_configuration, checkFails);
        var workflow = new BiliAccountPageWorkflow(_configuration, new LoginDouble(), guard);
        await workflow.QrLoginCompleteAsync(
            new BiliCookie(
                new Dictionary<string, string>
                {
                    ["DedeUserID"] = "123",
                    ["SESSDATA"] = "synthetic",
                    ["bili_jct"] = "synthetic",
                }
            )
        );
        Assert.Equal(1, guard.Calls);
        Assert.Equal("123", guard.LastUserId);
        Assert.True(guard.PersistedBeforeCheck);
        Assert.Contains("SESSDATA=synthetic", _configuration["BiliBiliCookies:0"]);
    }

    [Fact]
    public async Task UpdateAsync_NewCookieIsPersistedBeforeImmediateCheck()
    {
        var guard = new CaptureGuard(_configuration, false);
        var workflow = new BiliAccountPageWorkflow(_configuration, null!, guard);
        await workflow.UpdateAsync(0, Cookie);
        Assert.Equal(1, guard.Calls);
        Assert.Equal("123", guard.LastUserId);
        Assert.True(guard.PersistedBeforeCheck);
        Assert.Equal(Cookie, _configuration["BiliBiliCookies:0"]);
    }

    [Fact]
    public async Task AddAsync_ManualModeSavesWithoutAutomaticCheck()
    {
        _configuration["CookieCheck:AutoCheckEnabled"] = "false";
        var guard = new CaptureGuard(_configuration, false);
        await new BiliAccountPageWorkflow(_configuration, null!, guard).AddAsync(Cookie);
        Assert.Equal(0, guard.Calls);
        Assert.Equal(Cookie, _configuration["BiliBiliCookies:0"]);
    }

    private sealed class CaptureGuard(IConfiguration configuration, bool fail) : ICookieTaskGuard
    {
        public int Calls { get; private set; }
        public string? LastUserId { get; private set; }
        public bool PersistedBeforeCheck { get; private set; }

        public Task EnsureValidAsync(
            string userId,
            string cookie,
            CancellationToken token = default
        ) => throw new InvalidOperationException("A saved login requires an explicit recheck");

        public Task CheckNowAsync(string userId, string cookie, CancellationToken token = default)
        {
            Calls++;
            LastUserId = userId;
            PersistedBeforeCheck = cookie == configuration["BiliBiliCookies:0"];
            if (fail)
                throw new InvalidOperationException("synthetic transport failure");
            return Task.CompletedTask;
        }
    }

    private sealed class LoginDouble : ILoginDomainService
    {
        public Task<BiliCookie> SetCookieAsync(BiliCookie cookie, CancellationToken token) =>
            Task.FromResult(cookie);

        public Task<BiliCookie> LoginByQrCodeAsync(CancellationToken token) =>
            throw new NotSupportedException();

        public Task<QrLoginGenerateResult> GenerateQrCodeWebAsync(CancellationToken token) =>
            throw new NotSupportedException();

        public Task<QrLoginCheckResult> CheckQrLoginAsync(string key, CancellationToken token) =>
            throw new NotSupportedException();

        public Task SaveCookieToJsonFileAsync(BiliCookie cookie, CancellationToken token) =>
            throw new NotSupportedException();

        public Task<bool> SaveCookieToQinLongAsync(BiliCookie cookie, CancellationToken token) =>
            throw new NotSupportedException();

        public Task<bool> SaveCookieToBaihuAsync(BiliCookie cookie, CancellationToken token) =>
            throw new NotSupportedException();

        public Task<bool> SaveCookieToDaiDaiAsync(BiliCookie cookie, CancellationToken token) =>
            throw new NotSupportedException();
    }

    public void Dispose()
    {
        (_configuration as IDisposable)?.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Delete(recursive: true);
    }
}
