using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace BlazingQuartz.Core.Services
{
    internal class SchedulerDefinitionService : ISchedulerDefinitionService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private IEnumerable<IntervalUnit> _calendarIntervalUnits;
        private IEnumerable<IntervalUnit> _simpleIntervalUnits;
        private IEnumerable<MisfireAction>? _cronCalDailyMisfireActions;
        private IEnumerable<MisfireAction>? _simpleMisfireActions;
        private readonly BlazingQuartzCoreOptions _options;
        private readonly ILogger<SchedulerDefinitionService> _logger;
        private List<Type>? _allowedJobTypes;

        public SchedulerDefinitionService(
            ILogger<SchedulerDefinitionService> logger,
            ISchedulerFactory schedulerFactory,
            IOptions<BlazingQuartzCoreOptions> options
        )
        {
            _logger = logger;
            _schedulerFactory = schedulerFactory;
            _options = options.Value;
            Init();
        }

        [MemberNotNull(nameof(_calendarIntervalUnits))]
        [MemberNotNull(nameof(_simpleIntervalUnits))]
        private void Init()
        {
            _calendarIntervalUnits = new List<IntervalUnit>
            {
                IntervalUnit.Second,
                IntervalUnit.Minute,
                IntervalUnit.Hour,
                IntervalUnit.Day,
                IntervalUnit.Week,
                IntervalUnit.Month,
                IntervalUnit.Year,
            };
            _simpleIntervalUnits = new List<IntervalUnit>
            {
                IntervalUnit.Second,
                IntervalUnit.Minute,
                IntervalUnit.Hour,
            };
        }

        public IEnumerable<IntervalUnit> GetTriggerIntervalUnits(TriggerType triggerType)
        {
            switch (triggerType)
            {
                case TriggerType.Calendar:
                    return _calendarIntervalUnits;
                case TriggerType.Daily:
                case TriggerType.Simple:
                    return _simpleIntervalUnits;
                default:
                    return Enumerable.Empty<IntervalUnit>();
            }
        }

        public IEnumerable<MisfireAction> GetMisfireActions(TriggerType triggerType)
        {
            switch (triggerType)
            {
                case TriggerType.Cron:
                case TriggerType.Daily:
                case TriggerType.Calendar:
                    if (_cronCalDailyMisfireActions == null)
                    {
                        _cronCalDailyMisfireActions = new List<MisfireAction>
                        {
                            MisfireAction.SmartPolicy,
                            MisfireAction.DoNothing,
                            MisfireAction.IgnoreMisfirePolicy,
                            MisfireAction.FireOnceNow,
                        };
                    }
                    return _cronCalDailyMisfireActions;
                case TriggerType.Simple:
                    if (_simpleMisfireActions == null)
                    {
                        _simpleMisfireActions = new List<MisfireAction>
                        {
                            MisfireAction.SmartPolicy,
                            MisfireAction.FireNow,
                            MisfireAction.IgnoreMisfirePolicy,
                            MisfireAction.RescheduleNextWithExistingCount,
                            MisfireAction.RescheduleNextWithRemainingCount,
                            MisfireAction.RescheduleNowWithExistingRepeatCount,
                            MisfireAction.RescheduleNowWithRemainingRepeatCount,
                        };
                    }
                    return _simpleMisfireActions;
            }

            return Enumerable.Empty<MisfireAction>();
        }

        public IEnumerable<Type> GetJobTypes(bool reload = false)
        {
            if (_options.AllowedJobAssemblyFiles == null)
                return Enumerable.Empty<Type>();

            // use cached job types if already loaded
            if (_allowedJobTypes != null && !reload)
                return _allowedJobTypes;

            HashSet<string> disallowedJobs = new(
                _options.DisallowedJobTypes ?? Enumerable.Empty<string>()
            );

            if (_options.DisallowedJobTypes != null)
                _logger.LogInformation(
                    "{disallowedVar} was set. Will not load following job types {jobTypes}",
                    nameof(_options.DisallowedJobTypes),
                    _options.DisallowedJobTypes
                );

            var path =
                Path.GetDirectoryName(
                    Assembly.GetAssembly(typeof(SchedulerDefinitionService))!.Location
                ) ?? String.Empty;
            List<Type> jobTypes = new();
            foreach (var assemblyStr in _options.AllowedJobAssemblyFiles)
            {
                string assemblyPath = Path.Combine(path, assemblyStr + ".dll");
                try
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    if (assembly == null)
                    {
                        _logger.LogWarning(
                            "Cannot load allowed job assembly name '{assembly}'",
                            assemblyStr
                        );
                        continue;
                    }

                    jobTypes.AddRange(
                        assembly
                            .GetExportedTypes()
                            .Where(x =>
                                x.IsPublic
                                && x.IsClass
                                && !x.IsAbstract
                                && typeof(IJob).IsAssignableFrom(x)
                                && !disallowedJobs.Contains(x.FullName ?? string.Empty)
                            )
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to load allowed job assembly filename '{assembly}'",
                        assemblyStr
                    );
                    continue;
                }
            }
            if (!jobTypes.Any())
                return jobTypes;

            _allowedJobTypes = jobTypes;
            return _allowedJobTypes.AsReadOnly();
        }
    }
}
