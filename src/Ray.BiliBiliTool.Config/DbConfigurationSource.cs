using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Config
{
    public class DbConfigurationSource : IConfigurationSource
    {
        public bool ReloadOnChange { get; set; }
        public int ReloadDelay { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DbConfigurationProvider(this);
        }

        public Func<List<IDbConfigEntity>> GetDbListFunc;
    }
}
