namespace Ray.Serilog.Sinks.Batched;

public interface IPushService
{
    Task<HttpResponseMessage> PushMessageAsync(string message, string title = "");
}
