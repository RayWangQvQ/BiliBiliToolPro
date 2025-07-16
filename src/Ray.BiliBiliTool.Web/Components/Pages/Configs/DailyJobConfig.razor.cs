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

    private DailyTaskOptions _config = new();
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
            _config = DailyTaskOptionsMonitor.CurrentValue;
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
            var sqliteProvider = GetSqliteConfigurationProvider();
            if (sqliteProvider == null)
            {
                throw new InvalidOperationException("无法获取 SqliteConfigurationProvider");
            }

            var configValues = _config.ToConfigDictionary();

            sqliteProvider.BatchUpdateConfig(configValues);

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
