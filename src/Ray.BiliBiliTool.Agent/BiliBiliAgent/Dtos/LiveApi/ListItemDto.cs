using System.Text.Json;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;

public class GetListResponse
{
    public List<LiveSortTag> New_tags { get; set; } = [];

    public List<ListItemDto> List { get; set; } = [];

    public int Has_more { get; set; }
}

public class LiveSortTag
{
    public long Id { get; set; }

    public required string Name { get; set; }

    public string? Sort_type { get; set; }
}

public class ListItemDto
{
    public long Roomid { get; set; }

    public long Uid { get; set; }

    public required string Title { get; set; }

    public string ShortTitle
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Title) || Title.Length <= 10)
                return Title;

            return Title.Substring(0, 7) + "...";
        }
    }

    public required string Uname { get; set; }

    public long Parent_id { get; set; }

    public required string Parent_name { get; set; }

    public long Area_id { get; set; }

    public string? Area_name { get; set; }

    /// <summary>
    /// 挂件信息。形状偶发异常（[]/标量/null），用 JsonElement 容忍解析，避免整轮反序列化崩溃。
    /// </summary>
    /// <sample>1：百人成就</sample>
    /// <sample>2：天选时刻、新星主播</sample>
    public JsonElement? Pendant_info { get; set; }
}
