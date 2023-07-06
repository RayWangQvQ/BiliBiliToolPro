using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Config
{
    public class DbConfigurationProvider : ConfigurationProvider
    {
        private readonly DbConfigurationSource _source;

        public DbConfigurationProvider(DbConfigurationSource source)
        {
            _source = source;
            if (source.ReloadOnChange)
            {
                EntityChangeObserver.Instance.Changed += EntityChangeObserver_Changed;
            }
        }

        public override void Load()
        {
            Data = new Dictionary<string, string>();

            var items = _source.GetDbListFunc.Invoke();

            foreach (var item in items)
            {
                var key = item.ConfigKey;
                Data[key] = item.ConfigValue;
            }
        }

        private void EntityChangeObserver_Changed(object sender, EntityChangeEventArgs e)
        {
            if (e.Entity.GetType() != typeof(IDbConfigEntity))
                return;

            Thread.Sleep(_source.ReloadDelay);
            Load();
        }
    }
}
