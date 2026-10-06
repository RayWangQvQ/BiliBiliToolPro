using System.Reflection;

namespace Ray.BiliBiliTool.Web.UnitTests;

public static class LiveTaskTestSupport
{
    public sealed class BudgetClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 6, 23, 59, 0, TimeSpan.FromHours(8));

        public override DateTimeOffset GetUtcNow() => Now;
    }

    public class Proxy : DispatchProxy
    {
        public Func<string, object?[], object> Handler { get; set; } = null!;
        public Action<string>? Before { get; set; }

        protected override object Invoke(MethodInfo? method, object?[]? args)
        {
            Before?.Invoke(method!.Name);
            return Handler(method!.Name, args!);
        }

        public static T Create<T>(
            Func<string, object?[], object> handler,
            Action<string>? before = null
        )
            where T : class
        {
            var value = Create<T, Proxy>();
            var proxy = (Proxy)(object)value;
            proxy.Handler = handler;
            proxy.Before = before;
            return value;
        }
    }
}
