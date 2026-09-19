using System;
using System.IO;
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
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }
}