using Microsoft.EntityFrameworkCore;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Infrastructure.EF;

public class DbInitializer(BiliDbContext context)
{
    private const string DefaultUserName = "admin";
    private const string DefaultPassword = "BiliTool@2233";

    public async Task InitializeAsync()
    {
        await context.Database.MigrateAsync();

        await InitUserAsync();
    }

    private async Task InitUserAsync()
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var (hash, salt) = PasswordHelper.HashPassword(DefaultPassword);
        var adminUser = new User
        {
            Username = DefaultUserName,
            PasswordHash = hash,
            Salt = salt,
            Roles = ["Administrator"],
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }
}
