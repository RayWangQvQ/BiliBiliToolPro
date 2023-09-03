using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Config.Options
{
    public class VipBigPointTaskOptions
    {
        public bool IsEnable { get; set; } = true;

        public string Cron { get; set; }
    }
}
