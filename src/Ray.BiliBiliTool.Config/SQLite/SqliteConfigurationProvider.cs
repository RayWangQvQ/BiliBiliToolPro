using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Config.SQLite;

public class SqliteConfigurationProvider(SqliteConfigurationSource source) : ConfigurationProvider
{
    private readonly string _connectionString =
        source.ConnectionString ?? throw new ArgumentNullException(nameof(source.ConnectionString));
    private readonly string _tableName = source.TableName ?? "AppSettings";
    private readonly string _keyColumnName = source.KeyColumnName ?? "Key";
    private readonly string _valueColumnName = source.ValueColumnName ?? "Value";

    public override void Load()
    {
        Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        EnsureTableExists(connection);

        using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT [{_keyColumnName}], [{_valueColumnName}] FROM [{_tableName}]";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string key = reader.GetString(0);
            string value = reader.GetString(1);
            Data[key] = value;
        }
    }

    private void EnsureTableExists(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            $@"
            CREATE TABLE IF NOT EXISTS [{_tableName}] (
                [{_keyColumnName}] TEXT PRIMARY KEY,
                [{_valueColumnName}] TEXT NOT NULL
            )";
        command.ExecuteNonQuery();
    }

    public override void Set(string key, string? value)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            $@"
            INSERT OR REPLACE INTO [{_tableName}] ([{_keyColumnName}], [{_valueColumnName}])
            VALUES (@key, @value)";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        command.ExecuteNonQuery();

        Data[key] = value;
    }

    public void BatchSet(Dictionary<string, string> configValues)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        try
        {
            foreach (var kvp in configValues)
            {
                command.CommandText =
                    $@"
                    INSERT OR REPLACE INTO [{_tableName}] ([{_keyColumnName}], [{_valueColumnName}])
                    VALUES (@key, @value)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@key", kvp.Key);
                command.Parameters.AddWithValue("@value", kvp.Value ?? (object)DBNull.Value);
                command.ExecuteNonQuery();

                Data[kvp.Key] = kvp.Value;
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
