using Ray.DDD;

namespace Ray.BiliTool.Domain.BiliAccountAggregate.Entity
{
    public class BiliAccount : Entity<long>, IAggregateRoot, ISoftDelete
    {
        protected BiliAccount()
        {
        }

        public string CookieStr { get; set; }

        public bool Enable { get; set; } = true;

        public bool IsSoftDeleted { get; set; }
    }
}
