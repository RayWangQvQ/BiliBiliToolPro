using System.ComponentModel;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;

public enum FollowingsOrderType
{
    /// <summary>
    /// 最常访问频率倒序
    /// </summary>
    [DefaultValue("attention")]
    AttentionDesc,

    /// <summary>
    /// 关注时间倒序
    /// </summary>
    [DefaultValue("")]
    TimeDesc,
}
