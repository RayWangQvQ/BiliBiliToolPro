using System;
using Quartz;

namespace BlazingQuartz.Core.Helpers
{
    public static class CronExpressionHelper
    {
        public static bool IsValidExpression(string cronExpression)
        {
            if (string.IsNullOrEmpty(cronExpression))
            {
                return false;
            }

            try
            {
                _ = new CronExpression(cronExpression);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
