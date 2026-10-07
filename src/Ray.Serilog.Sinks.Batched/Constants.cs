namespace Ray.Serilog.Sinks.Batched;

public static class Constants
{
    public const string DefaultOutputTemplate = "{Message:lj}{NewLine}{Exception}";

    public const string GroupPropertyKey = "GroupKey";
}
