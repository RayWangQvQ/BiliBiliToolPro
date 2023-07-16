using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ray.DDD;
using Ray.Repository.EntityFramework;

namespace Ray.BiliTool.Repository.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositoryModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            services.AddIdentityDefaultRepositories<BiliDbContext>(optionsAction =>
            {
                optionsAction.UseSqlite(connectionString);
            });

            services.AddTransient<IdentitySeed>();

            return services;
        }

        public static void Seed(this IServiceProvider sp)
        {
            var seed = sp.GetRequiredService<IdentitySeed>();
            seed.SeedAsync().Wait();
        } 
    }
}
