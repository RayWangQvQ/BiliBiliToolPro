using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Components.Pages.Schedules;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;
using Xunit;
using Key = BlazingQuartz.Core.Models.Key;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class LogsDialogTextTests : TestContext
{
    public LogsDialogTextTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<ILogsDialogWorkflow>(new EmptyWorkflow());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData("<script>window.synthetic = true</script>")]
    [InlineData("<img src=x onerror='window.synthetic = true'>")]
    [InlineData("<svg onload='window.synthetic = true'></svg>")]
    [InlineData("<a href='javascript:window.synthetic = true'>link</a>")]
    [InlineData("<iframe srcdoc='<script>window.synthetic = true</script>'></iframe>")]
    [InlineData("</span><style>body{display:none}</style><span>")]
    [InlineData("<form action='https://example.invalid'><input name='secret'></form>")]
    [InlineData("&#60;script&#62;&amp; synthetic <b>bold</b>")]
    public async Task UntrustedMessage_IsRenderedAsText(string message)
    {
        var cut = await RenderLogs(message);
        var element = cut.Find(".log-message");
        Assert.Equal(message, element.TextContent);
        Assert.Empty(element.Children);
        Assert.Empty(cut.FindAll("script, img, svg[onload], a[href], iframe, style, form, input"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task MissingMessage_RendersAnEmptyTextEntry(string? message)
    {
        var cut = await RenderLogs(message);
        Assert.Equal("", cut.Find(".log-message").TextContent);
    }

    [Theory]
    [InlineData("first\nsecond")]
    [InlineData("first\r\nsecond")]
    [InlineData("first\rsecond")]
    public async Task MultilineMessage_PreservesLineBreaksWithoutHtml(string message)
    {
        var cut = await RenderLogs(message);
        Assert.Equal(message.ReplaceLineEndings("\n"), cut.Find(".log-message").TextContent);
        Assert.Empty(cut.FindAll(".log-message br"));
    }

    [Fact]
    public async Task PlainMessageAndException_PreserveTextAndLevel()
    {
        var cut = await RenderLogs(
            "进度 2/10 < 3 & 4 > 1",
            "<img src=x onerror='synthetic'>\nline 2"
        );
        Assert.Equal("进度 2/10 < 3 & 4 > 1", cut.FindAll(".log-message")[0].TextContent);
        Assert.Equal(
            "<img src=x onerror='synthetic'>\nline 2",
            cut.FindAll(".log-message")[1].TextContent
        );
        Assert.Empty(cut.FindAll("img"));
        Assert.Equal("WARN", cut.Find(".log-level-warning").TextContent);
    }

    [Fact]
    public async Task UpdatedMessage_StillUsesTextRendering()
    {
        var cut = await RenderLogs("initial");
        await SetLogs(cut, "<img src=x onerror='synthetic'>");
        Assert.Equal("<img src=x onerror='synthetic'>", cut.Find(".log-message").TextContent);
        Assert.Empty(cut.FindAll("img"));
    }

    private async Task<IRenderedComponent<MudDialogProvider>> RenderLogs(
        string? message,
        string? exception = null
    )
    {
        var provider = RenderComponent<MudDialogProvider>();
        var parameters = new DialogParameters<LogsDialog>
        {
            { component => component.JobKey, new Key("synthetic-job") },
            { component => component.TriggerKey, new Key("synthetic-trigger") },
        };
        var service = ((IServiceProvider)Services).GetRequiredService<IDialogService>();
        await provider.InvokeAsync(() => service.ShowAsync<LogsDialog>("日志", parameters));
        provider.WaitForAssertion(() => Assert.Single(provider.FindComponents<LogsDialog>()));
        await SetLogs(provider, message, exception);
        return provider;
    }

    private static async Task SetLogs(
        IRenderedComponent<MudDialogProvider> provider,
        string? message,
        string? exception = null
    )
    {
        var cut = provider.FindComponent<LogsDialog>();
        await provider.InvokeAsync(() =>
        {
            typeof(LogsDialog)
                .GetField("_logs", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(
                    cut.Instance,
                    new List<BiliLogs>
                    {
                        new()
                        {
                            Timestamp = new DateTime(2026, 1, 1),
                            Level = "warning",
                            RenderedMessage = message,
                            Exception = exception,
                        },
                    }
                );
            typeof(LogsDialog)
                .GetField("_loading", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(cut.Instance, false);
            cut.Render();
        });
        provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".log-message")));
    }

    private sealed class EmptyWorkflow : ILogsDialogWorkflow
    {
        public Task<string?> GetLatestRunInstanceIdAsync(string jobName, string? triggerName) =>
            Task.FromResult<string?>(null);

        public Task<List<BiliLogs>> GetLogsForRunAsync(
            string fireInstanceId,
            int maxCount,
            CancellationToken ct
        ) => Task.FromResult(new List<BiliLogs>());
    }
}
