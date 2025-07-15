using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Config.SQLite;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class DailyJobConfig : ComponentBase
{
    [Inject]
    private IOptionsMonitor<DailyTaskOptions> DailyTaskOptionsMonitor { get; set; } = null!;

    [Inject]
    private IConfiguration Configuration { get; set; } = null!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    private DailyTaskOptions _config = new();
    private string? _chargeComment;
    private bool _isLoading = true;
    private string? _saveMessage;
    private bool _saveSuccess;

    protected override async Task OnInitializedAsync()
    {
        await LoadConfig();
    }

    private async Task LoadConfig()
    {
        _isLoading = true;
        _saveMessage = null;

        try
        {
            // 从配置中加载当前值
            _config = new DailyTaskOptions
            {
                IsWatchVideo = bool.Parse(GetConfigValue("DailyTaskConfig:IsWatchVideo", "true")),
                IsShareVideo = bool.Parse(GetConfigValue("DailyTaskConfig:IsShareVideo", "true")),
                IsDonateCoinForArticle = bool.Parse(
                    GetConfigValue("DailyTaskConfig:IsDonateCoinForArticle", "false")
                ),
                NumberOfCoins = int.Parse(GetConfigValue("DailyTaskConfig:NumberOfCoins", "5")),
                NumberOfProtectedCoins = int.Parse(
                    GetConfigValue("DailyTaskConfig:NumberOfProtectedCoins", "0")
                ),
                SaveCoinsWhenLv6 = bool.Parse(
                    GetConfigValue("DailyTaskConfig:SaveCoinsWhenLv6", "false")
                ),
                SelectLike = bool.Parse(GetConfigValue("DailyTaskConfig:SelectLike", "false")),
                SupportUpIds = GetConfigValue("DailyTaskConfig:SupportUpIds", ""),
                DayOfAutoCharge = int.Parse(
                    GetConfigValue("DailyTaskConfig:DayOfAutoCharge", "-1")
                ),
                AutoChargeUpId = GetConfigValue("DailyTaskConfig:AutoChargeUpId", ""),
                DayOfReceiveVipPrivilege = int.Parse(
                    GetConfigValue("DailyTaskConfig:DayOfReceiveVipPrivilege", "-1")
                ),
                DayOfExchangeSilver2Coin = int.Parse(
                    GetConfigValue("DailyTaskConfig:DayOfExchangeSilver2Coin", "-1")
                ),
                DevicePlatform = GetConfigValue("DailyTaskConfig:DevicePlatform", "android"),
                CustomComicId = long.Parse(
                    GetConfigValue("DailyTaskConfig:CustomComicId", "27355")
                ),
                CustomEpId = long.Parse(GetConfigValue("DailyTaskConfig:CustomEpId", "381662")),
                Cron = GetConfigValue("DailyTaskConfig:Cron", "0 0 6 * * ?"),
            };

            _chargeComment = GetConfigValue("DailyTaskConfig:ChargeComment", "");
        }
        catch (Exception ex)
        {
            _saveMessage = $"加载配置失败: {ex.Message}";
            _saveSuccess = false;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleValidSubmit()
    {
        _isLoading = true;
        _saveMessage = null;

        try
        {
            // 获取 SqliteConfigurationProvider 实例来保存配置
            var sqliteProvider = GetSqliteConfigurationProvider();
            if (sqliteProvider == null)
            {
                throw new InvalidOperationException("无法获取 SqliteConfigurationProvider");
            }

            // 保存所有配置项
            sqliteProvider.Set(
                "DailyTaskConfig:IsWatchVideo",
                _config.IsWatchVideo.ToString().ToLower()
            );
            sqliteProvider.Set(
                "DailyTaskConfig:IsShareVideo",
                _config.IsShareVideo.ToString().ToLower()
            );
            sqliteProvider.Set(
                "DailyTaskConfig:IsDonateCoinForArticle",
                _config.IsDonateCoinForArticle.ToString().ToLower()
            );
            sqliteProvider.Set("DailyTaskConfig:NumberOfCoins", _config.NumberOfCoins.ToString());
            sqliteProvider.Set(
                "DailyTaskConfig:NumberOfProtectedCoins",
                _config.NumberOfProtectedCoins.ToString()
            );
            sqliteProvider.Set(
                "DailyTaskConfig:SaveCoinsWhenLv6",
                _config.SaveCoinsWhenLv6.ToString().ToLower()
            );
            sqliteProvider.Set(
                "DailyTaskConfig:SelectLike",
                _config.SelectLike.ToString().ToLower()
            );
            sqliteProvider.Set("DailyTaskConfig:SupportUpIds", _config.SupportUpIds ?? "");
            sqliteProvider.Set(
                "DailyTaskConfig:DayOfAutoCharge",
                _config.DayOfAutoCharge.ToString()
            );
            sqliteProvider.Set("DailyTaskConfig:AutoChargeUpId", _config.AutoChargeUpId ?? "");
            sqliteProvider.Set("DailyTaskConfig:ChargeComment", _chargeComment ?? "");
            sqliteProvider.Set(
                "DailyTaskConfig:DayOfReceiveVipPrivilege",
                _config.DayOfReceiveVipPrivilege.ToString()
            );
            sqliteProvider.Set(
                "DailyTaskConfig:DayOfExchangeSilver2Coin",
                _config.DayOfExchangeSilver2Coin.ToString()
            );
            sqliteProvider.Set("DailyTaskConfig:DevicePlatform", _config.DevicePlatform);
            sqliteProvider.Set("DailyTaskConfig:CustomComicId", _config.CustomComicId.ToString());
            sqliteProvider.Set("DailyTaskConfig:CustomEpId", _config.CustomEpId.ToString());
            sqliteProvider.Set("DailyTaskConfig:Cron", _config.Cron ?? "0 0 6 * * ?");

            _saveMessage = "配置保存成功！";
            _saveSuccess = true;
        }
        catch (Exception ex)
        {
            _saveMessage = $"保存配置失败: {ex.Message}";
            _saveSuccess = false;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private string GetConfigValue(string key, string defaultValue)
    {
        var value = Configuration[key];
        return !string.IsNullOrEmpty(value) ? value : defaultValue;
    }

    private SqliteConfigurationProvider? GetSqliteConfigurationProvider()
    {
        if (Configuration is IConfigurationRoot configRoot)
        {
            foreach (var provider in configRoot.Providers)
            {
                if (provider is SqliteConfigurationProvider sqliteProvider)
                {
                    return sqliteProvider;
                }
            }
        }
        return null;
    }
}
