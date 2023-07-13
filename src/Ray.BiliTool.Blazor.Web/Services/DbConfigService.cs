using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliTool.Domain;
using Ray.DDD;
using Ray.Repository;

namespace Ray.BiliTool.Blazor.Web.Services
{
    public interface IDbConfigService
    {
        Task<Dictionary<string, string>> GetConfigsAsync(IEnumerable<string> configKeys);
        Task AddOrUpdateConfigsAsync(Dictionary<string,string> configs);
    }

    public class DbConfigService : IDbConfigService
    {
        private readonly IConfiguration _config;
        private readonly IBaseRepository<DbConfig, long> _repo;

        public DbConfigService(IConfiguration config, IBaseRepository<DbConfig,long> repo)
        {
            _config = config;
            _repo = repo;
        }

        public async Task<Dictionary<string, string>> GetConfigsAsync(IEnumerable<string> configKeys)
        {
            var dic=new Dictionary<string, string>();

            foreach (var configKey in configKeys)
            {
                var configValue = _config[configKey];
                dic.Add(configKey, configValue);
            }

            return dic;
        }

        public async Task AddOrUpdateConfigsAsync(Dictionary<string, string> configs)
        {
            if (configs == null)return;

            foreach (var config in configs)
            {
                var exist = await _repo.FindAsync(x => x.ConfigKey == config.Key);

                if (exist!=null)
                {
                    exist.UpdateConfig(config.Value);
                }
                else
                {
                    await _repo.InsertAsync(new DbConfig(config.Key, config.Value));
                }
                continue;
            }

            await _repo.UnitOfWork.SaveChangesAsync();
        }
    }
}
