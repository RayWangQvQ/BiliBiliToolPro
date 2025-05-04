using System;

namespace BlazingQuartz.Jobs.Abstractions
{
    public static class JobDataMapKeys
    {
        public const string ExecutionDetails = "__execDetails";
        public const string IsSuccess = "__isSuccess";
        public const string ReturnCode = "__returnCode";
    }
}
