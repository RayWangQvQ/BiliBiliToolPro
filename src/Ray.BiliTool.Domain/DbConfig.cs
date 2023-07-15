using System.ComponentModel;
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
            Enable = true;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string ConfigKey { get; set; }

        public string ConfigValue { get; set; }

        [DefaultValue(true)]
        public bool Enable { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }

        public void UpdateConfig(string configValue, bool enable = true)
        {
            ConfigValue = configValue;
            Enable = enable;
            UpdateTime = DateTime.UtcNow.AddHours(8);
        }
    }
}
