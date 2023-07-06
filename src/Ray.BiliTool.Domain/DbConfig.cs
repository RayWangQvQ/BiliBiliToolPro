using Ray.DDD;
using System.ComponentModel.DataAnnotations.Schema;
using Ray.BiliBiliTool.Config;

namespace Ray.BiliTool.Domain
{
    public class DbConfig : Entity<long>, IDbConfigEntity
    {
        public DbConfig(string configKey, string configValue)
        {
            ConfigKey = configKey;
            ConfigValue = configValue;
            CreateTime = DateTime.UtcNow.AddHours(8);
            UpdateTime = DateTime.UtcNow.AddHours(8);
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; }

        public string ConfigKey { get; set; }

        public string ConfigValue { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }

        public void UpdateConfig(string configValue)
        {
            ConfigValue = configValue;
            UpdateTime = DateTime.UtcNow.AddHours(8);
        }
    }
}
