using Microsoft.JSInterop;
using MudBlazor;

namespace Ray.BiliBiliTool.Web.Components.Layout;

public static class AppTheme
{
    public static async Task<bool> IsDarkModePreferredAsync(IJSRuntime jsRuntime)
    {
        var stored = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "bilitool-dark");
        return stored is "1" or "true";
    }

    public static MudTheme Create() =>
        new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#C73869",
                Secondary = "#516278",
                AppbarBackground = "#ffffff",
                AppbarText = "#242638",
                Background = "#f7f8fb",
                Surface = "#ffffff",
                DrawerBackground = "#ffffff",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#FF8BAD",
                Secondary = "#AFBCD0",
                AppbarBackground = "#20232d",
                AppbarText = "#f5f5fa",
                Background = "#171923",
                Surface = "#242733",
                DrawerBackground = "#20232d",
            },
        };
}
