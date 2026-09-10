using Ray.BiliBiliTool.Infrastructure.EF;

namespace Ray.BiliBiliTool.Web.Extensions;

public static class WebHostStartupExtensions
{
    public static async Task InitializeBiliToolAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await dbInitializer.InitializeAsync();
    }
}
