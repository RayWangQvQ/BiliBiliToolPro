using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Config.SQLite;
using Xunit;

namespace Ray.BiliBiliTool.Config.IntegrationTests;

public class SqliteConfigurationProviderTests
{
    [Fact]
    public void AddSqlite_MissingDatabaseDirectory_CreatesDirectory()
    {
        string rootDirectory = Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));
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

    [Fact]
    public void Set_ExistingKey_PersistsReplacementAcrossReload()
    {
        string databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");
        try
        {
            var config = new ConfigurationBuilder()
                .AddSqlite($"Data Source={databasePath}")
                .Build();
            config["Answer"] = "first";
            config["Answer"] = "second";

            var reloaded = new ConfigurationBuilder()
                .AddSqlite($"Data Source={databasePath}")
                .Build();
            Assert.Equal("second", reloaded["Answer"]);
            Assert.Equal("second", config["Answer"]);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }
}
