using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Config
{
    public interface IDbConfigEntity
    {
        public string ConfigKey { get; set; }

        public string ConfigValue { get; set; }

        public bool Enable { get; set; }
    }
}
