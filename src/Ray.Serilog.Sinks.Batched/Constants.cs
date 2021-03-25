using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.Serilog.Sinks.Batched
{
    public static class Constants
    {
        public const string DefaultOutputTemplate =
            "{Message:lj}{NewLine}{Exception}";

        public const string DefaultContainsTrigger = "开始推送";
    }
}
