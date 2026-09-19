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

        // Mirrors Program.cs: cookies.json supplies the list, SQLite is layered on top
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = FirstCookie,
                    ["BiliBiliCookies:1"] = SecondCookie,
                }
            )
            .AddSqlite(
                $"Data Source={Path.Combine(_rootDirectory, "BiliBiliTool.db")}",
                tableName: Ray.BiliBiliTool.Config.Constants.SqliteTableName
            )
            .Build();

        // loginDomainService is only reachable from the QR login methods
        _workflow = new BiliAccountPageWorkflow(_configuration, null!);
    }

    [Fact]
    public async Task GetAllAccountsAsync_ReturnsAccountsInConfiguredOrder()
    {
        var accounts = await _workflow.GetAllAccountsAsync();

        accounts.Select(a => a.CookieStr).Should().Equal(FirstCookie, SecondCookie);
        accounts.Select(a => a.UserId).Should().Equal("111", "222");
    }

    [Fact]
    public async Task UpdateAsync_PersistedValueIsReadBackThroughConfiguration()
    {
        await _workflow.UpdateAsync(1, "DedeUserID=999; SESSDATA=zzz");

        var accounts = await _workflow.GetAllAccountsAsync();

        accounts.Should().HaveCount(2);
        accounts[1].CookieStr.Should().Be("DedeUserID=999; SESSDATA=zzz");
    }

    [Fact]
    public async Task AddAsync_AppendsWithoutDisturbingExistingAccounts()
    {
        await _workflow.AddAsync("DedeUserID=333; SESSDATA=ccc");

        var accounts = await _workflow.GetAllAccountsAsync();

        accounts
            .Select(a => a.CookieStr)
            .Should()
            .Equal(FirstCookie, SecondCookie, "DedeUserID=333; SESSDATA=ccc");
    }

    [Fact]
    public async Task ReorderAsync_SwapsPositionsPersistently()
    {
        await _workflow.ReorderAsync(0, 1);

        var accounts = await _workflow.GetAllAccountsAsync();

        accounts.Select(a => a.CookieStr).Should().Equal(SecondCookie, FirstCookie);
    }

    [Fact]
    public async Task DeleteAsync_DroppedAccountNoLongerResurfacesFromTheJsonSource()
    {
        await _workflow.DeleteAsync(0);

        var accounts = await _workflow.GetAllAccountsAsync();

        accounts.Should().NotContain(a => a.CookieStr == FirstCookie);
        accounts[0].CookieStr.Should().Be(SecondCookie);
    }

    [Fact]
    public async Task Writes_UseHierarchicalKeysThatConfigurationCanBind()
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

        keys.Should().Equal("BiliBiliCookies:0");
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
