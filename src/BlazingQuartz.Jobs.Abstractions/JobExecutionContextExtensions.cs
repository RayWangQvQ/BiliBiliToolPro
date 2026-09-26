using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Quartz;

namespace BlazingQuartz.Jobs.Abstractions
{
    public static class JobExecutionContextExtensions
    {
        // MergedJobDataMap is copied per firing and never written back, so these keys stay the
        // job -> listener channel they were under Quartz 3's removed IJobExecutionContext.Put/Get.
        public static IJobExecutionContext SetReturnCode(
            this IJobExecutionContext context,
            string value
        )
        {
            context.MergedJobDataMap[JobDataMapKeys.ReturnCode] = value;
            return context;
        }

        public static IJobExecutionContext SetReturnCode(
            this IJobExecutionContext context,
            int value
        )
        {
            context.MergedJobDataMap[JobDataMapKeys.ReturnCode] = value.ToString();
            return context;
        }

        public static IJobExecutionContext SetExecutionDetails(
            this IJobExecutionContext context,
            string execDetails
        )
        {
            context.MergedJobDataMap[JobDataMapKeys.ExecutionDetails] = execDetails;
            return context;
        }

        public static IJobExecutionContext SetIsSuccess(
            this IJobExecutionContext context,
            bool success
        )
        {
            context.MergedJobDataMap[JobDataMapKeys.IsSuccess] = success;
            return context;
        }

        public static string? GetReturnCode(this IJobExecutionContext context)
        {
            context.MergedJobDataMap.TryGetValue(JobDataMapKeys.ReturnCode, out var val);
            if (val != null)
                return Convert.ToString(val, CultureInfo.InvariantCulture);
            return null;
        }

        public static string? GetExecutionDetails(this IJobExecutionContext context)
        {
            context.MergedJobDataMap.TryGetValue(JobDataMapKeys.ExecutionDetails, out var val);
            if (val != null)
                return Convert.ToString(val, CultureInfo.InvariantCulture);

            return null;
        }

        public static bool? GetIsSuccess(this IJobExecutionContext context)
        {
            context.MergedJobDataMap.TryGetValue(JobDataMapKeys.IsSuccess, out var value);
            if (value == null)
                return null;
            return Convert.ToBoolean(value);
        }

        public static DataMapValue? GetDataMapValue(this IJobExecutionContext context, string key)
        {
            var value = context.MergedJobDataMap.GetString(key);
            return DataMapValue.Create(value);
        }

        public static DataMapValue? GetDataMapValue(this JobDataMap dataMap, string key)
        {
            if (dataMap.TryGetString(key, out var value))
            {
                return DataMapValue.Create(value);
            }

            return null;
        }

        public static string? GetReturnCodeAndResult(this IJobExecutionContext context)
        {
            var returnCode = context.GetReturnCode();
            var strBldr = new StringBuilder();
            if (!string.IsNullOrEmpty(returnCode))
            {
                strBldr.Append($"Return {returnCode}. ");
            }
            var result = context.Result?.ToString();
            if (!string.IsNullOrEmpty(result))
            {
                strBldr.Append(result);
            }
            return strBldr.ToString();
        }
    }
}
