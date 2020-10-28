using System;

namespace Ray.BiliBiliTool.Infrastructure.Extensions
{
    public static class DateTimeExtension
    {
        /// <summary>
        /// 获取当前月份的最后一天
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime LastDayOfMonth(this DateTime dateTime)
        {
            return dateTime.AddDays(1 - dateTime.Day)
                .AddMonths(1)
                .AddDays(-1);
        }
    }
}
