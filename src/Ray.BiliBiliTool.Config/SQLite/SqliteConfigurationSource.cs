using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Config.SQLite;

public class SqliteConfigurationSource : IConfigurationSource
{
    public string? ConnectionString { get; set; }
    public string? TableName { get; set; }
    public string? KeyColumnName { get; set; }
    public string? ValueColumnName { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SqliteConfigurationProvider(this);
    }
}
