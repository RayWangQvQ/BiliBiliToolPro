using Ray.BiliBiliTool.Infrastructure.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class Silver2CoinResponse
    {
        public long Coin { get; set; }

        public long Gold { get; set; }

        public long Silver { get; set; }

        public string Tid { get; set; }
    }
}
