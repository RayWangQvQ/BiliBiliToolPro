using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Config.SQLite;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class Silver2CoinTaskConfig : ComponentBase
{
    [Inject]
    private IOptionsMonitor<Silver2CoinTaskOptions> Silver2CoinTaskOptionsMonitor { get; set; } =
        null!;

    [Inject]
    private IConfiguration Configuration { get; set; } = null!;

    private Silver2CoinTaskOptions _config = new();
    private bool _isLoading = true;
    private string? _saveMessage;
    private bool _saveSuccess;

    protected override async Task OnInitializedAsync()
    {
        await LoadConfigAsync();
    }

    private Task LoadConfigAsync()
    {
        _isLoading = true;
        _saveMessage = null;

        try
        {
            _config = Silver2CoinTaskOptionsMonitor.CurrentValue;
        }
        catch (Exception ex)
        {
            _saveMessage = $"Failed to load configuration: {ex.Message}";
            _saveSuccess = false;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private Task HandleValidSubmitAsync()
    {
        _isLoading = true;
        _saveMessage = null;

        try
        {
            var sqliteProvider = GetSqliteConfigurationProvider();
            if (sqliteProvider == null)
            {
                throw new InvalidOperationException("Unable to get SqliteConfigurationProvider");
            }

            var configValues = _config.ToConfigDictionary();

            sqliteProvider.BatchSet(configValues);

            _saveMessage = "Configuration saved successfully!";
            _saveSuccess = true;
        }
        catch (Exception ex)
        {
            _saveMessage = $"Failed to save configuration: {ex.Message}";
            _saveSuccess = false;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }

        return Task.CompletedTask;
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
