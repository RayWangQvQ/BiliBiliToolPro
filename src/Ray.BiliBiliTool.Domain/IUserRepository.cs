namespace Ray.BiliBiliTool.Domain;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username);
    Task<User> GetAdminAsync();
    Task UpdateAsync(User user);
}
