namespace Ray.BiliBiliTool.Config.Options;

public class MangaTaskOptions : BaseConfigOptions
{
    public override string SectionName => "MangaTaskConfig";

    /// <summary>
    /// 自定义漫画阅读 comic_id
    /// </summary>
    public long CustomComicId { get; set; } = 27355;

    /// <summary>
    /// 自定义漫画阅读 ep_id
    /// </summary>
    public long CustomEpId { get; set; } = 381662;

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                { $"{SectionName}:{nameof(CustomComicId)}", CustomComicId.ToString() },
                { $"{SectionName}:{nameof(CustomEpId)}", CustomEpId.ToString() },
            }
        );
    }
}
