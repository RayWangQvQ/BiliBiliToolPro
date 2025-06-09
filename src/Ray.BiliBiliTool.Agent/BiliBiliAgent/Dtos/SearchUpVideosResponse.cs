namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

public class SearchUpVideosResponse
{
    public UpContent? List { get; set; }

    public required BiliPageResult Page { get; set; }
}

public class UpContent
{
    public List<UpVideoInfo> Vlist { get; set; } = [];
}

public class UpVideoInfo
{
    public long Aid { get; set; }

    public string? Author { get; set; }

    public required string Bvid { get; set; }

    public required string Title { get; set; }

    /// <summary>
    /// 视频时长
    /// <sample>61:05</sample>
    /// <sample>00:15</sample>
    /// </summary>
    public required string Length { get; set; }

    /// <summary>
    /// 视频时长的秒数
    /// </summary>
    public int? Duration
    {
        get
        {
            int? result = null;

            try
            {
                var list = Length.Split(':');
                var min = int.Parse(list[0]);
                var sec = int.Parse(list[1]);
                return min * 60 + sec;
            }
            catch (Exception)
            {
                //throw;
            }
            return result;
        }
    }
}
