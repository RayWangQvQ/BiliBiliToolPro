using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        Task<Dictionary<string, string>> GetConfigsByConfigurationAsync(IEnumerable<string> configKeys);
        Task<List<DbConfig>> GetConfigsByDbAsync(Expression<Func<DbConfig, bool>> predicate);
        Task AddOrUpdateConfigsAsync(Dictionary<string, string> configs);
        Task AddOrUpdateConfigsAsync(DbConfig dbConfig);
        Task DeleteConfigsAsync(Expression<Func<DbConfig, bool>> predicate);
    }

    public class DbConfigService : IDbConfigService
    {
        private readonly IConfiguration _config;
        private readonly IBaseRepository<DbConfig, long> _repo;

        public DbConfigService(IConfiguration config, IBaseRepository<DbConfig, long> repo)
        {
            _config = config;
            _repo = repo;
        }

        public async Task<Dictionary<string, string>> GetConfigsByConfigurationAsync(IEnumerable<string> configKeys)
        {
            var dic = new Dictionary<string, string>();

            foreach (var configKey in configKeys)
            {
                var configValue = _config[configKey];
                dic.Add(configKey, configValue);
            }

            return dic;
        }

        public async Task<List<DbConfig>> GetConfigsByDbAsync(Expression<Func<DbConfig, bool>> predicate)
        {
            return await _repo.GetListAsync(predicate);
        }

        public async Task AddOrUpdateConfigsAsync(Dictionary<string, string> configs)
        {
            if (configs == null) return;

            foreach (var config in configs)
            {
                DbConfig exist = await _repo.FindAsync(x => x.ConfigKey == config.Key);

                if (exist != null)
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

        public async Task AddOrUpdateConfigsAsync(DbConfig dbConfig)
        {
            if (dbConfig == null) return;

            DbConfig exist = await _repo.FindAsync(x => x.ConfigKey == dbConfig.ConfigKey);

            if (exist != null)
            {
                exist.UpdateConfig(dbConfig.ConfigValue, dbConfig.Enable);
            }
            else
            {
                await _repo.InsertAsync(new DbConfig(dbConfig.ConfigKey, dbConfig.ConfigValue));
            }

            await _repo.UnitOfWork.SaveChangesAsync();
        }

        public async Task DeleteConfigsAsync(Expression<Func<DbConfig, bool>> predicate)
        {
            var target = await _repo.GetAsync(predicate);

            await _repo.DeleteAsync(target, true);
        }
    }
}
