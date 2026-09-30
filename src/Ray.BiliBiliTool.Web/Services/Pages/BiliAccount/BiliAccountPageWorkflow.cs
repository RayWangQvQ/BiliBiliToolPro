using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;

public class BiliAccountPageWorkflow(
    IConfiguration configuration,
    ILoginDomainService loginDomainService
) : IBiliAccountPageWorkflow
{
    private readonly IConfigurationRoot _configurationRoot =
        configuration as IConfigurationRoot
        ?? throw new InvalidOperationException(
            "IConfigurationRoot not available — cannot access Providers or Reload()"
        );

    public static void CompactStoredAccounts(IConfigurationRoot configuration)
    {
        var provider = configuration.Providers.OfType<SqliteConfigurationProvider>().Single();
        var indices = provider
            .GetChildKeys([], "BiliBiliCookies")
            .Select(key => int.TryParse(key, out var index) ? index : -1)
            .Where(index => index >= 0)
            .Order()
            .ToList();
        var cookies = indices
            .Select(index =>
            {
                provider.TryGet($"BiliBiliCookies:{index}", out var value);
                return value;
            })
            .Where(value => !string.IsNullOrEmpty(value))
            .ToList();

        if (
            indices.Count == cookies.Count
            && indices.SequenceEqual(Enumerable.Range(0, cookies.Count))
        )
            return;

        var values = cookies
            .Select((value, index) => new { Key = $"BiliBiliCookies:{index}", Value = value! })
            .ToDictionary(item => item.Key, item => item.Value);
        var keysToDelete = indices
            .Where(index => index >= cookies.Count)
            .Select(index => $"BiliBiliCookies:{index}")
            .ToList();
        provider.BatchSet(values, keysToDelete);
        configuration.Reload();
    }

    public Task<List<BiliAccountDto>> GetAllAccountsAsync()
    {
        var cookieList = _configurationRoot.GetSection("BiliBiliCookies").Get<List<string>>() ?? [];
        var accounts = new List<BiliAccountDto>();

        for (int i = 0; i < cookieList.Count; i++)
        {
            var cookieStr = cookieList[i];
            var userId = ParseUserId(cookieStr);
            accounts.Add(new BiliAccountDto(i, userId, cookieStr));
        }

        return Task.FromResult(accounts);
    }

    public Task AddAsync(string cookieStr)
    {
        var provider =
            GetSqliteProvider()
            ?? throw new InvalidOperationException("SqliteConfigurationProvider not found");

        var currentCount =
            _configurationRoot.GetSection("BiliBiliCookies").Get<List<string>>()?.Count ?? 0;
        provider.Set($"BiliBiliCookies:{currentCount}", cookieStr);
        ReloadConfiguration();
        return Task.CompletedTask;
    }

    public Task UpdateAsync(int index, string cookieStr)
    {
        var provider =
            GetSqliteProvider()
            ?? throw new InvalidOperationException("SqliteConfigurationProvider not found");

        provider.Set($"BiliBiliCookies:{index}", cookieStr);
        ReloadConfiguration();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int index)
    {
        var provider =
            GetSqliteProvider()
            ?? throw new InvalidOperationException("SqliteConfigurationProvider not found");

        var cookieList = _configurationRoot.GetSection("BiliBiliCookies").Get<List<string>>() ?? [];
        if (index < 0 || index >= cookieList.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var newCount = cookieList.Count - 1;

        var rekeyDict = new Dictionary<string, string>();
        for (int i = index + 1; i < cookieList.Count; i++)
        {
            rekeyDict[$"BiliBiliCookies:{i - 1}"] = cookieList[i];
        }

        provider.BatchSet(rekeyDict, [$"BiliBiliCookies:{newCount}"]);
        ReloadConfiguration();
        return Task.CompletedTask;
    }

    public Task ReorderAsync(int fromIndex, int toIndex)
    {
        var provider =
            GetSqliteProvider()
            ?? throw new InvalidOperationException("SqliteConfigurationProvider not found");

        var cookieList = _configurationRoot.GetSection("BiliBiliCookies").Get<List<string>>() ?? [];

        if (fromIndex < 0 || fromIndex >= cookieList.Count)
            throw new ArgumentOutOfRangeException(nameof(fromIndex));
        if (toIndex < 0 || toIndex >= cookieList.Count)
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        if (fromIndex == toIndex)
            return Task.CompletedTask;

        // Swap the two keys atomically via BatchSet
        var swapDict = new Dictionary<string, string>
        {
            [$"BiliBiliCookies:{fromIndex}"] = cookieList[toIndex],
            [$"BiliBiliCookies:{toIndex}"] = cookieList[fromIndex],
        };

        provider.BatchSet(swapDict);
        ReloadConfiguration();
        return Task.CompletedTask;
    }

    public Task<QrLoginGenerateResult> QrLoginGenerateAsync()
    {
        return loginDomainService.GenerateQrCodeWebAsync(CancellationToken.None);
    }

    public Task<QrLoginCheckResult> QrLoginPollAsync(string qrcodeKey)
    {
        return loginDomainService.CheckQrLoginAsync(qrcodeKey, CancellationToken.None);
    }

    public async Task QrLoginCompleteAsync(BiliCookie rawCookie)
    {
        // Per D-02: enrich cookie via SetCookieAsync, then save to SQLite
        var enriched = await loginDomainService.SetCookieAsync(rawCookie, CancellationToken.None);
        await AddAsync(enriched.CookieStr);
    }

    private SqliteConfigurationProvider? GetSqliteProvider()
    {
        foreach (var provider in _configurationRoot.Providers)
        {
            if (provider is SqliteConfigurationProvider sqliteProvider)
                return sqliteProvider;
        }
        return null;
    }

    private void ReloadConfiguration()
    {
        _configurationRoot.Reload();
    }

    private static string ParseUserId(string cookieStr)
    {
        try
        {
            var items = cookieStr.Split(";", StringSplitOptions.TrimEntries);
            foreach (var item in items)
            {
                var eqIndex = item.IndexOf("=", StringComparison.Ordinal);
                if (eqIndex <= 0)
                    continue;

                var key = item[..eqIndex].Trim();
                var value = item[(eqIndex + 1)..].Trim();

                if (key == "DedeUserID" && !string.IsNullOrEmpty(value))
                    return value;
            }
        }
        catch
        {
            // Parsing failed — fall through to unknown
        }

        return "(unknown)";
    }
}
