using System.ComponentModel;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public enum VipStatus
    {
        [Description("无/过期")]
        Disable = 0,

        [Description("正常")]
        Enable = 1,
    }
}
