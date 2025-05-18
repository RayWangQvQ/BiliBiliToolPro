using System;

namespace BlazingQuartz.Core.Services
{
    public interface ISchedulerDefinitionService
    {
        IEnumerable<IntervalUnit> GetTriggerIntervalUnits(TriggerType triggerType);
        IEnumerable<MisfireAction> GetMisfireActions(TriggerType triggerType);

        /// <summary>
        /// Return available IJob implementations
        /// </summary>
        /// <param name="reload"></param>
        /// <returns></returns>
        IEnumerable<Type> GetJobTypes(bool reload = false);
    }
}
