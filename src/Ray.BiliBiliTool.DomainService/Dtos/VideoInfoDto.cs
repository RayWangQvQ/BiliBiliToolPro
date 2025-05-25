namespace Ray.BiliBiliTool.DomainService.Dtos;

public class VideoInfoDto
{
    public required string Aid { get; set; }

    public required string Bvid { get; set; }

    public long Cid { get; set; }

    public required string Title { get; set; }

    public int? Duration { get; set; }

    /// <summary>
    /// 是否转载
    /// <sample>1：原创</sample>
    /// <sample>2：转载</sample>
    /// </summary>
    public int Copyright { get; set; }
}
