using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public enum VipType
    {
        [Description("无")]
        None = 0,

        [Description("月度大会员")]
        Mensual = 1,

        [Description("年度大会员")]
        Annual = 2
    }
}
