using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure.EF;

/// <summary>
/// 仅供 `dotnet ef` 设计时使用（生成/应用迁移）。
/// 应用运行时不走这条路径 —— 运行时的连接串由 appsettings / 环境变量提供。
/// </summary>
public class BiliDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BiliDbContext>
{
    public BiliDbContext CreateDbContext(string[] args)
    {
        // 生成迁移只需要模型，不会真的连库，用一个占位连接串即可。
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Sqlite"] = "Data Source=designtime.db",
                }
            )
            .Build();

        return new BiliDbContext(config);
    }
}
