using Microsoft.EntityFrameworkCore;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Infrastructure.EF;

public class UserRepository(IDbContextFactory<BiliDbContext> dbFactory) : IUserRepository
{
    public async Task<User?> FindByUsernameAsync(string username)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> GetAdminAsync()
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.Users.FirstAsync(u => u.Id == 1);
    }

    public async Task UpdateAsync(User user)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }
}
