using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Config.SQLite;

public static class SqliteConfigurationExtensions
{
    public static IConfigurationBuilder AddSqlite(
        this IConfigurationBuilder builder,
        string connectionString,
        string tableName = "AppSettings",
        string keyColumnName = "Key",
        string valueColumnName = "Value"
    )
    {
        return builder.Add(
            new SqliteConfigurationSource
            {
                ConnectionString = connectionString,
                TableName = tableName,
                KeyColumnName = keyColumnName,
                ValueColumnName = valueColumnName,
            }
        );
    }
}
