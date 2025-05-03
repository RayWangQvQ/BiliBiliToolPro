using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.SQLite;
using Microsoft.EntityFrameworkCore;

namespace Ray.BiliBiliTool.Web;

public class BiliDbContext(IConfiguration config) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(config.GetConnectionString("Sqlite"));
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddQuartz(builder => builder.UseSqlite("QRTZ_"));
    }
}
