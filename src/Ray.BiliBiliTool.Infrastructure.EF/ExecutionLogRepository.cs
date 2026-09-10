using Microsoft.EntityFrameworkCore;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Infrastructure.EF;

public class ExecutionLogRepository(IDbContextFactory<BiliDbContext> dbFactory)
    : IExecutionLogRepository
{
    public async Task<string?> GetLatestRunInstanceIdAsync(string jobName, string triggerName)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var execution = await context
            .ExecutionLogs.Where(x => x.JobName == jobName && x.TriggerName == triggerName)
            .OrderByDescending(x => x.FireTimeUtc)
            .FirstOrDefaultAsync();
        return execution?.RunInstanceId;
    }

    public async Task<List<BiliLogs>> GetLogsForRunAsync(
        string fireInstanceId,
        int maxCount,
        CancellationToken ct
    )
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context
            .BiliLogs.Where(x => x.FireInstanceIdComputed == fireInstanceId)
            .OrderBy(l => l.Timestamp)
            .Take(maxCount)
            .ToListAsync(ct);
    }
}
