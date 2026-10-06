using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Config.SQLite;

namespace Ray.BiliBiliTool.Web.Services;

public sealed record LiveMedalExclusionChange(long AnchorId, bool Excluded);

public interface ILiveMedalParticipationWorkflow
{
    Task<string> SetExcludedAsync(long anchorId, bool excluded, CancellationToken token = default);
}

public sealed class LiveMedalParticipationWorkflow(IConfiguration configuration)
    : ILiveMedalParticipationWorkflow
{
    public const string ExclusionKey = "LiveFansMedalTaskConfig:ExcludedAnchorIds";
    private static readonly object Gate = new();

    public Task<string> SetExcludedAsync(
        long anchorId,
        bool excluded,
        CancellationToken token = default
    )
    {
        if (anchorId <= 0)
            throw new ArgumentOutOfRangeException(nameof(anchorId));
        lock (Gate)
        {
            token.ThrowIfCancellationRequested();
            var root =
                configuration as IConfigurationRoot
                ?? throw new InvalidOperationException("Configuration root is unavailable");
            var provider =
                root.Providers.OfType<SqliteConfigurationProvider>().FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "SQLite configuration provider is unavailable"
                );
            root.Reload();
            var ids = new LiveFansMedalTaskOptions
            {
                ExcludedAnchorIds = configuration[ExclusionKey] ?? "",
            }.GetExcludedAnchorIds();
            if (excluded)
                ids.Add(anchorId);
            else
                ids.Remove(anchorId);
            var value = string.Join(",", ids.Order());
            provider.Set(ExclusionKey, value);
            root.Reload();
            return Task.FromResult(value);
        }
    }
}
