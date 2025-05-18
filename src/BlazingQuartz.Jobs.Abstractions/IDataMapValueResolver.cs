namespace BlazingQuartz.Jobs.Abstractions
{
    public interface IDataMapValueResolver
    {
        string? Resolve(DataMapValue? dmv);
    }
}
