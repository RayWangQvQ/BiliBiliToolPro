using Microsoft.EntityFrameworkCore;
using Ray.BiliTool.Domain;
using Ray.Repository.EntityFramework;

namespace Ray.BiliTool.Repository
{
    public class BiliDbContext: RayIdentityDbContext<BiliDbContext>
    {
        public BiliDbContext(DbContextOptions<BiliDbContext> options) :base(options)
        {
        }

        public virtual DbSet<DbConfig> DbConfigs { get; set; }
    }
}
