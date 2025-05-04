using System;
using BlazingQuartz.Core.Models;
using Microsoft.EntityFrameworkCore;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.EF;

namespace BlazingQuartz.Core.Services
{
    public class ExecutionLogService : IExecutionLogService
    {
        private readonly IDbContextFactory<BiliDbContext> _contextFactory;

        public ExecutionLogService(IDbContextFactory<BiliDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<PagedList<ExecutionLog>> GetLatestExecutionLog(
            string jobName,
            string jobGroup,
            string? triggerName,
            string? triggerGroup,
            PageMetadata? pageMetadata = null,
            long firstLogId = 0,
            HashSet<LogType>? logTypes = null
        )
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var q = context.ExecutionLogs.Where(l =>
                    l.JobName == jobName && l.JobGroup == jobGroup
                );

                if (triggerName is not null)
                {
                    q = q.Where(l =>
                        l.TriggerName == triggerName && l.TriggerGroup == triggerGroup
                    );
                }

                if (firstLogId > 0)
                {
                    // to avoid incorrect page data
                    q = q.Where(l => l.LogId <= firstLogId);
                }

                if (logTypes != null)
                {
                    q = q = q.Where(l => logTypes.Contains(l.LogType));
                }

                var ordered = q.OrderByDescending(l => l.DateAddedUtc)
                    .ThenByDescending(l => l.FireTimeUtc);
                if (pageMetadata == null)
                {
                    return new PagedList<ExecutionLog>(await ordered.ToListAsync());
                }

                PageMetadata newPageMetadata = pageMetadata;
                if (pageMetadata.Page == 0)
                {
                    // if first page, get the total records
                    var totalRecords = await q.CountAsync();
                    newPageMetadata = pageMetadata with { TotalCount = totalRecords };
                }

                var result = await ordered
                    .Skip(pageMetadata.Page * pageMetadata.PageSize)
                    .Take(pageMetadata.PageSize)
                    .ToListAsync();
                return new PagedList<ExecutionLog>(result, newPageMetadata);
            }
        }

        public async Task<PagedList<ExecutionLog>> GetExecutionLogs(
            ExecutionLogFilter? filter = null,
            PageMetadata? pageMetadata = null,
            long firstLogId = 0
        )
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                IQueryable<ExecutionLog> q = context.ExecutionLogs;
                if (filter != null)
                {
                    if (filter.JobName != null)
                    {
                        q = q.Where(l => l.JobName == filter.JobName);
                    }

                    if (filter.JobGroup != null)
                    {
                        q = q.Where(l => l.JobGroup == filter.JobGroup);
                    }

                    if (filter.TriggerName != null)
                    {
                        q = q.Where(l => l.TriggerName == filter.TriggerName);
                    }

                    if (filter.TriggerGroup != null)
                    {
                        q = q.Where(l => l.TriggerGroup == filter.TriggerGroup);
                    }

                    if (filter.LogTypes != null && filter.LogTypes.Any())
                    {
                        q = q.Where(l => filter.LogTypes.Contains(l.LogType));
                    }

                    if (filter.DateAddedStartUtc != null)
                    {
                        q = q.Where(l => l.DateAddedUtc >= filter.DateAddedStartUtc);
                    }

                    if (filter.DateAddedEndUtc != null)
                    {
                        q = q.Where(l => l.DateAddedUtc < filter.DateAddedEndUtc);
                    }

                    if (filter.ErrorOnly)
                    {
                        q = q.Where(l =>
                            (l.IsException ?? false) || (l.IsSuccess.HasValue && !l.IsSuccess.Value)
                        );
                    }

                    if (filter.MessageContains != null)
                    {
                        var likeStr = $"%{filter.MessageContains}%";
                        q = q.Where(l =>
                            EF.Functions.Like(l.JobName ?? string.Empty, likeStr)
                            || EF.Functions.Like(l.TriggerName ?? string.Empty, likeStr)
                            || EF.Functions.Like(l.Result ?? string.Empty, likeStr)
                            || EF.Functions.Like(l.ErrorMessage ?? string.Empty, likeStr)
                            || (
                                l.ExecutionLogDetail != null
                                && (
                                    EF.Functions.Like(
                                        l.ExecutionLogDetail.ExecutionDetails ?? string.Empty,
                                        likeStr
                                    )
                                    || EF.Functions.Like(
                                        l.ExecutionLogDetail.ErrorStackTrace ?? string.Empty,
                                        likeStr
                                    )
                                    || (
                                        l.ExecutionLogDetail.ErrorCode != null
                                        && l.ExecutionLogDetail.ErrorCode.Value.ToString()
                                            == filter.MessageContains
                                    )
                                )
                            )
                        );
                    }

                    if (!filter.IncludeSystemJobs)
                    {
                        q = q.Where(l =>
                            !(
                                l.TriggerGroup == Constants.SYSTEM_GROUP
                                || l.JobGroup == Constants.SYSTEM_GROUP
                            )
                        );
                    }
                }

                IOrderedQueryable<ExecutionLog> ordered;
                if (filter != null && filter.IsAscending)
                {
                    ordered = q.OrderBy(l => l.DateAddedUtc).ThenBy(l => l.FireTimeUtc);
                }
                else
                {
                    if (firstLogId > 0)
                    {
                        // to avoid incorrect page data for descing order
                        q = q.Where(l => l.LogId <= firstLogId);
                    }
                    ordered = q.OrderByDescending(l => l.DateAddedUtc)
                        .ThenByDescending(l => l.FireTimeUtc);
                }

                if (pageMetadata == null)
                {
                    return new PagedList<ExecutionLog>(await ordered.ToListAsync());
                }

                PageMetadata newPageMetadata = pageMetadata;
                if (pageMetadata.Page == 0)
                {
                    // if first page, get the total records
                    var totalRecords = await q.CountAsync();
                    newPageMetadata = pageMetadata with { TotalCount = totalRecords };
                }

                var result = await ordered
                    .Skip(pageMetadata.Page * pageMetadata.PageSize)
                    .Take(pageMetadata.PageSize)
                    .ToListAsync();
                return new PagedList<ExecutionLog>(result, newPageMetadata);
            }
        }

        public async Task<IList<string>> GetJobNames()
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context
                    .ExecutionLogs.Where(l => l.LogType != LogType.System)
                    .Select(l => l.JobName ?? string.Empty)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();
            }
        }

        public async Task<IList<string>> GetJobGroups()
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context
                    .ExecutionLogs.Where(l => l.LogType != LogType.System)
                    .Select(l => l.JobGroup ?? string.Empty)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();
            }
        }

        public async Task<IList<string>> GetTriggerNames()
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context
                    .ExecutionLogs.Where(l => l.LogType != LogType.System)
                    .Select(l => l.TriggerName ?? string.Empty)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();
            }
        }

        public async Task<IList<string>> GetTriggerGroups()
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context
                    .ExecutionLogs.Where(l => l.LogType != LogType.System)
                    .Select(l => l.TriggerGroup ?? string.Empty)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();
            }
        }

        public async Task<JobExecutionStatusSummaryModel> GetJobExecutionStatusSummary(
            DateTimeOffset? startTimeUtc,
            DateTimeOffset? endTimeUtc = null
        )
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var q = context.ExecutionLogs.Where(l => l.LogType == LogType.ScheduleJob);
                if (startTimeUtc.HasValue)
                {
                    q = q.Where(l => l.DateAddedUtc >= startTimeUtc.Value);
                }
                if (endTimeUtc.HasValue)
                {
                    q = q.Where(l => l.DateAddedUtc < endTimeUtc.Value);
                }

                var statusList = q.Select(l => new
                {
                    DateAddedUtc = l.DateAddedUtc,
                    ExecutionStatus = (l.IsException ?? false)
                        ?
                        // has exception
                        JobExecutionStatus.Failed
                        :
                        // vetoed?
                        (
                            (l.IsVetoed ?? false)
                                ? JobExecutionStatus.Vetoed
                                :
                                // is success null?
                                (
                                    l.IsSuccess.HasValue
                                        ? (
                                            l.IsSuccess.Value
                                                ? JobExecutionStatus.Success
                                                : JobExecutionStatus.Failed
                                        )
                                        : JobExecutionStatus.Executing
                                )
                        ),
                });

                var statusGroup = await statusList
                    .GroupBy(l => l.ExecutionStatus)
                    .Select(g => new
                    {
                        EarliestDateAdded = g.Min(l => l.DateAddedUtc),
                        ExecutionStatus = g.Key,
                        Count = g.Count(),
                    })
                    .ToListAsync();

                if (!statusGroup.Any())
                    return new();

                return new JobExecutionStatusSummaryModel
                {
                    StartDateTimeUtc = statusGroup.Min(s => s.EarliestDateAdded).DateTime,
                    Data = statusGroup
                        .Select(s => new KeyValuePair<JobExecutionStatus, int>(
                            s.ExecutionStatus,
                            s.Count
                        ))
                        .ToList(),
                };
            }
        }
    }
}
