namespace Ray.BiliBiliTool.Infrastructure.Extensions;

public static class ICollectionExtensions
{
    public static void AddIfNotExist<T>(this ICollection<T> source, T add)
    {
        if (!source.Any(x => x != null && x.Equals(add)))
            source.Add(add);
    }

    public static void AddIfNotExist<T>(this ICollection<T> source, T add, Func<T, bool> exist)
    {
        if (!source.Any(exist))
            source.Add(add);
    }
}
