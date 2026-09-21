using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.EF;

namespace TodayTaskTest;

public class TaskRecordWriterTest : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"bilitool-test-{Guid.NewGuid():N}.db"
    );
    private readonly ServiceProvider _provider;
    private readonly IDbContextFactory<BiliDbContext> _factory;

    public TaskRecordWriterTest()
    {
        // BiliDbContext 的构造函数需要 IConfiguration，且连接串在 OnConfiguring 里从配置取，
        // 因此直接用 DI 构造工厂，而不是手搓 DbContextOptions。
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath};Cache=Shared",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddDbContextFactory<BiliDbContext>();
        _provider = services.BuildServiceProvider();
        _factory = _provider.GetRequiredService<IDbContextFactory<BiliDbContext>>();

        using var db = _factory.CreateDbContext();
        // 用 EnsureCreated 而不是 Migrate()：既有的 AddBiliLogs 迁移里有不带 ifExists 的
        // DropTable("bili_logs")，全新库从零 Migrate 会报 "no such table: bili_logs"（历史遗留问题，
        // 与本次改动无关，线上库已应用过全部迁移所以不受影响）。
        // 迁移本身是否能正确升级线上库，由「真实库副本」的验证步骤单独覆盖。
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _provider.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task 写入一条记录后能按账号与日期查出来()
    {
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance);

        await writer.WriteAsync(
            1001,
            "DailyTaskAppService",
            "DonateCoin",
            TaskRecordStatus.Success,
            null,
            TaskRecordTrigger.Manual
        );

        await using var db = await _factory.CreateDbContextAsync();
        var record = Assert.Single(db.TaskRecords.Where(r => r.UserId == 1001));
        Assert.Equal("DailyTaskAppService", record.TaskKey);
        Assert.Equal("DonateCoin", record.TaskItemKey);
        Assert.Equal(TaskRecordStatus.Success, record.Status);
        Assert.Equal(TaskRecordTrigger.Manual, record.Trigger);
        Assert.Equal(DateTimeOffset.Now.ToString("yyyy-MM-dd"), record.RecordDate);
    }

    [Fact]
    public async Task 失败信息超过512字时被截断()
    {
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance);
        var longMessage = new string('错', 600);

        await writer.WriteAsync(
            1002,
            "DailyTaskAppService",
            null,
            TaskRecordStatus.Failed,
            longMessage,
            TaskRecordTrigger.Scheduled
        );

        await using var db = await _factory.CreateDbContextAsync();
        var record = Assert.Single(db.TaskRecords.Where(r => r.UserId == 1002));
        Assert.Equal(512, record.Message!.Length);
    }
}
