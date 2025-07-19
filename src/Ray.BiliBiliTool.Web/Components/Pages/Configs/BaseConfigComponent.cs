using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Config.SQLite;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public abstract class BaseConfigComponent<T> : ComponentBase
    where T : class, IConfigOptions, new()
{
    [Inject]
    protected IConfiguration Configuration { get; set; } = null!;

    protected T _config = new();
    protected bool _isLoading = true;
    protected string? _saveMessage;
    protected bool _saveSuccess;

    protected abstract IOptionsMonitor<T> OptionsMonitor { get; }

    protected override async Task OnInitializedAsync()
    {
        await LoadConfigAsync();
    }

    protected Task LoadConfigAsync()
    {
        _isLoading = true;
        _saveMessage = null;

        try
        {
            _config = OptionsMonitor.CurrentValue;
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

    protected Task HandleValidSubmitAsync()
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
