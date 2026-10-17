using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class BiliAccountPageWorkflowTests : IDisposable
{
    private const string FirstCookie = "DedeUserID=111; SESSDATA=aaa";
    private const string SecondCookie = "DedeUserID=222; SESSDATA=bbb";

    private readonly string _rootDirectory = Path.Combine(
        Path.GetTempPath(),
        Guid.NewGuid().ToString("N")
    );
    private readonly IConfigurationRoot _configuration;
    private readonly BiliAccountPageWorkflow _workflow;

    public BiliAccountPageWorkflowTests()
    {
        Directory.CreateDirectory(_rootDirectory);

        _configuration = new ConfigurationBuilder()
            .AddSqlite(
                $"Data Source={Path.Combine(_rootDirectory, "BiliBiliTool.db")}",
                tableName: Ray.BiliBiliTool.Config.Constants.SqliteTableName
            )
            .Build();

        var provider = _configuration.Providers.OfType<SqliteConfigurationProvider>().Single();
        provider.BatchSet(
            new Dictionary<string, string>
            {
                ["BiliBiliCookies:0"] = FirstCookie,
                ["BiliBiliCookies:1"] = SecondCookie,
            }
        );
        _configuration.Reload();

        // loginDomainService is only reachable from the QR login methods
        _workflow = new BiliAccountPageWorkflow(_configuration, null!);
    }

    [Fact]
    public async Task GetAllAccountsAsync_ConfiguredAccounts_ReturnsConfiguredOrder()
    {
        var accounts = await _workflow.GetAllAccountsAsync();

        accounts.Select(a => a.CookieStr).Should().Equal(FirstCookie, SecondCookie);
        accounts.Select(a => a.UserId).Should().Equal("111", "222");
    }

    [Fact]
    public async Task UpdateAsync_ChangedCookie_PersistsValueToConfiguration()
    {
        await _workflow.UpdateAsync(1, "DedeUserID=999; SESSDATA=zzz");

        var accounts = await _workflow.GetAllAccountsAsync();

        accounts.Should().HaveCount(2);
        accounts[1].CookieStr.Should().Be("DedeUserID=999; SESSDATA=zzz");
    }

    [Fact]
    public async Task AddAsync_NewCookie_AppendsWithoutDisturbingExistingAccounts()
    {
        await _workflow.AddAsync("DedeUserID=333; SESSDATA=ccc");

        var accounts = await _workflow.GetAllAccountsAsync();

        accounts
            .Select(a => a.CookieStr)
            .Should()
            .Equal(FirstCookie, SecondCookie, "DedeUserID=333; SESSDATA=ccc");
    }

    [Fact]
    public async Task ReorderAsync_TwoExistingAccounts_PersistsSwappedPositions()
    {
        await _workflow.ReorderAsync(0, 1);

        var accounts = await _workflow.GetAllAccountsAsync();

        accounts.Select(a => a.CookieStr).Should().Equal(SecondCookie, FirstCookie);
    }

    [Fact]
    public async Task DeleteAsync_FirstAccount_RemovesRowAndCompactsAccounts()
    {
        await _workflow.DeleteAsync(0);

        var accounts = await _workflow.GetAllAccountsAsync();

        accounts.Select(a => a.CookieStr).Should().Equal(SecondCookie);

        _configuration.Reload();
        (await _workflow.GetAllAccountsAsync())
            .Select(a => a.CookieStr)
            .Should()
            .Equal(SecondCookie);

        await _workflow.AddAsync("DedeUserID=333; SESSDATA=ccc");
        (await _workflow.GetAllAccountsAsync())
            .Select(a => a.CookieStr)
            .Should()
            .Equal(SecondCookie, "DedeUserID=333; SESSDATA=ccc");

        ReadCookieRows()
            .Should()
            .Equal(
                new KeyValuePair<string, string>("BiliBiliCookies:0", SecondCookie),
                new KeyValuePair<string, string>(
                    "BiliBiliCookies:1",
                    "DedeUserID=333; SESSDATA=ccc"
                )
            );
    }

    [Fact]
    public async Task DeleteAsync_LastAccount_LeavesNoPlaceholder()
    {
        await _workflow.DeleteAsync(1);
        await _workflow.DeleteAsync(0);

        _configuration.Reload();
        (await _workflow.GetAllAccountsAsync()).Should().BeEmpty();
        ReadCookieRows().Should().BeEmpty();
    }

    [Fact]
    public async Task CompactStoredAccounts_LegacyBlankRows_KeepsRemainingAccounts()
    {
        var provider = _configuration.Providers.OfType<SqliteConfigurationProvider>().Single();
        provider.BatchSet(
            new Dictionary<string, string>
            {
                ["BiliBiliCookies:0"] = "",
                ["BiliBiliCookies:1"] = SecondCookie,
                ["BiliBiliCookies:2"] = "",
                ["BiliBiliCookies:3"] = "DedeUserID=333; SESSDATA=ccc",
            }
        );
        _configuration.Reload();

        BiliAccountPageWorkflow.CompactStoredAccounts(_configuration);
        (await _workflow.GetAllAccountsAsync())
            .Select(a => a.CookieStr)
            .Should()
            .Equal(SecondCookie, "DedeUserID=333; SESSDATA=ccc");
        ReadCookieRows()
            .Should()
            .Equal(
                new KeyValuePair<string, string>("BiliBiliCookies:0", SecondCookie),
                new KeyValuePair<string, string>(
                    "BiliBiliCookies:1",
                    "DedeUserID=333; SESSDATA=ccc"
                )
            );
    }

    [Fact]
    public async Task UpdateAsync_ChangedCookie_WritesBindableHierarchicalKeys()
    {
        await _workflow.UpdateAsync(0, "DedeUserID=111; SESSDATA=changed");

        using var connection = new SqliteConnection(
            $"Data Source={Path.Combine(_rootDirectory, "BiliBiliTool.db")}"
        );
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Key] FROM bili_appsettings";
        using var reader = command.ExecuteReader();

        var keys = new List<string>();
        while (reader.Read())
            keys.Add(reader.GetString(0));

        keys.Should().Equal("BiliBiliCookies:0", "BiliBiliCookies:1");
    }

    private List<KeyValuePair<string, string>> ReadCookieRows()
    {
        using var connection = new SqliteConnection(
            $"Data Source={Path.Combine(_rootDirectory, "BiliBiliTool.db")}"
        );
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT [Key], [Value] FROM bili_appsettings WHERE [Key] LIKE 'BiliBiliCookies:%' ORDER BY [Key]";
        using var reader = command.ExecuteReader();

        var rows = new List<KeyValuePair<string, string>>();
        while (reader.Read())
            rows.Add(new KeyValuePair<string, string>(reader.GetString(0), reader.GetString(1)));
        return rows;
    }

    public void Dispose()
    {
        // 连接池会在 Dispose 后继续持有文件句柄，需清空后才能删除临时目录
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
