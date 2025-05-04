using System;
using Quartz;

namespace BlazingQuartz.Core.Helpers
{
    public static class CronExpressionHelper
    {
        public static bool IsValidExpression(string cronExpression)
        {
            return CronExpression.IsValidExpression(cronExpression);
        }
    }
}
