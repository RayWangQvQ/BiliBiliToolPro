namespace Ray.BiliBiliTool.Agent;

public static class Constants
{
    public const string Channel = "bili";
}

public static class RelationApiConstant
{
    /// <summary>
    /// GetTags接口中的Referer
    /// {0}为UserId
    /// </summary>
    public const string GetTagsReferer = "https://space.bilibili.com/{0}/fans/follow";

    /// <summary>
    /// CopyUpsToGroup接口中的Referer
    /// {0}为UserId
    /// </summary>
    public const string CopyReferer = "https://space.bilibili.com/{0}/fans/follow?tagid=-1";

    /// <summary>
    /// ModifyRelation接口种的Referer
    /// </summary>
    public const string ModifyReferer = "https://space.bilibili.com/{0}/fans/follow?tagid={1}";
}
