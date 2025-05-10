using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Infrastructure.EF;

public class BiliDbContext(IConfiguration config) : DbContext
{
    public DbSet<ExecutionLog> ExecutionLogs { get; set; }
    public DbSet<BiliLogs> BiliLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(config.GetConnectionString("Sqlite"));
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddQuartz(builder => builder.UseSqlite("QRTZ_"));

        AddSqliteDateTimeOffsetSupport(modelBuilder);

        modelBuilder
            .Entity<ExecutionLog>()
            .OwnsOne(
                l => l.ExecutionLogDetail,
                e =>
                {
                    e.ToTable("bili_execution_log_details");
                    e.WithOwner().HasForeignKey("LogId");
                }
            );

        modelBuilder.Entity<ExecutionLog>().HasIndex(l => l.RunInstanceId).IsUnique();

        // for housekeeping or system log display
        modelBuilder.Entity<ExecutionLog>().HasIndex(l => new { l.DateAddedUtc, l.LogType });

        // joining with job
        modelBuilder
            .Entity<ExecutionLog>()
            .HasIndex(l => new
            {
                l.TriggerName,
                l.TriggerGroup,
                l.JobName,
                l.JobGroup,
                l.DateAddedUtc,
            });

        modelBuilder.Entity<ExecutionLog>().Property(e => e.LogType).HasConversion<string>();

        modelBuilder.Entity<BiliLogs>(entity =>
        {
            entity
                .Property<string>(x => x.FireInstanceIdComputed) // 定义一个影子属性
                .HasComputedColumnSql(
                    "json_extract(Properties, '$.FireInstanceId')",
                    stored: false
                ); // stored: true 表示持久化存储，利于索引；false (或省略) 为虚拟列

            entity
                .HasIndex(x => x.FireInstanceIdComputed) // 在计算列上创建索引
                .HasDatabaseName("IX_Logs_FireInstanceIdComputed");
        });
    }

    private void AddSqliteDateTimeOffsetSupport(ModelBuilder builder)
    {
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
            // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
            // To work around this, when the Sqlite database provider is used, all model properties of type DateTimeOffset
            // use the DateTimeOffsetToBinaryConverter
            // Based on: https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
            // This only supports millisecond precision, but should be sufficient for most use cases.
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var properties = entityType
                    .ClrType.GetProperties()
                    .Where(p =>
                        p.PropertyType == typeof(DateTimeOffset)
                        || p.PropertyType == typeof(DateTimeOffset?)
                    );
                foreach (var property in properties)
                {
                    builder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}
