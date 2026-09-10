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
        provider.Set($"BiliBiliCookies__{currentCount}", cookieStr);
        ReloadConfiguration();
        return Task.CompletedTask;
    }

    public Task UpdateAsync(int index, string cookieStr)
    {
        var provider =
            GetSqliteProvider()
            ?? throw new InvalidOperationException("SqliteConfigurationProvider not found");

        provider.Set($"BiliBiliCookies__{index}", cookieStr);
        ReloadConfiguration();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int index)
    {
        var provider =
            GetSqliteProvider()
            ?? throw new InvalidOperationException("SqliteConfigurationProvider not found");

        var cookieList = _configurationRoot.GetSection("BiliBiliCookies").Get<List<string>>() ?? [];
        var newCount = cookieList.Count - 1;

        // Re-key all higher indices down by 1
        var rekeyDict = new Dictionary<string, string>();
        for (int i = index + 1; i < cookieList.Count; i++)
        {
            rekeyDict[$"BiliBiliCookies__{i - 1}"] = cookieList[i];
        }

        if (rekeyDict.Count > 0)
            provider.BatchSet(rekeyDict);

        // Delete the old last key
        provider.Set($"BiliBiliCookies__{newCount}", string.Empty);
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
            [$"BiliBiliCookies__{fromIndex}"] = cookieList[toIndex],
            [$"BiliBiliCookies__{toIndex}"] = cookieList[fromIndex],
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
