using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Config.SQLite;
using Xunit;

namespace ConfigTest;

public class SqliteConfigurationProviderTests
{
    [Fact]
    public void AddSqlite_creates_the_missing_database_directory()
    {
        string rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string databasePath = Path.Combine(rootDirectory, "config", "BiliBiliTool.db");

        try
        {
            new ConfigurationBuilder().AddSqlite($"Data Source={databasePath}").Build();

            Assert.True(File.Exists(databasePath));
        }
        finally
        {
            // 连接池会在 Dispose 后继续持有文件句柄，需清空后才能删除临时目录
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }
}
