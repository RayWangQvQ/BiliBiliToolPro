using Microsoft.Extensions.Hosting;

namespace Ray.BiliBiliTool.Host.IntegrationTests.Support;

public static class ConsoleHostBuilder
{
    public static IHost CreateHost(params string[] args)
    {
        return Ray.BiliBiliTool.Console.Program.CreateHost(args);
    }
}
