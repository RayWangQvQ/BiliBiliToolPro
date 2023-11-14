using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.DDD;

namespace Ray.BiliTool.Domain.BiliAccount.Entity
{
    public class BiliAccountAggregate : Entity<long>, ISoftDelete
    {
        public string CookieStr { get; set; }

        public bool Enable { get; set; } = true;

        public bool IsSoftDeleted { get; set; }

        public void LoginByScan()
        {

        }
    }
}
