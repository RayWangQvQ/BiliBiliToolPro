namespace BlazingQuartz.Core.History
{
    public interface IExecutionLogRawSqlProvider
    {
        string DeleteLogsByDays { get; }
    }
}
