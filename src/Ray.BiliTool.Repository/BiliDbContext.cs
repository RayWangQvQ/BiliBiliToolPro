using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Ray.BiliBiliTool.Config;
using Ray.BiliTool.Domain;
using Ray.DDD;
using Ray.Repository.EntityFramework;

namespace Ray.BiliTool.Repository
{
    public class BiliDbContext : RayIdentityDbContext<BiliDbContext>
    {
        public BiliDbContext(DbContextOptions<BiliDbContext> options) : base(options)
        {
        }

        public override int SaveChanges()
        {
            return this.SaveChangesAsync().Result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var needReloadConfigs = IsConfigEntityChanged();
            var re = await base.SaveChangesAsync(cancellationToken);
            if (needReloadConfigs)
            {
                OnDbConfigEntityChange();
            }
            return re;
        }

        private bool IsConfigEntityChanged()
        {
            return ChangeTracker
                .Entries()
                .Where(x => x.Entity is DbConfig)
                .Any(i => i.State is EntityState.Modified
                    or EntityState.Added
                    or EntityState.Deleted);
        }

        private void OnDbConfigEntityChange()
        {
            EntityChangeObserver.Instance.OnChanged(new EntityChangeEventArgs(new DbConfig("", "")));
        }

        public virtual DbSet<DbConfig> DbConfigs { get; set; }
    }
}
