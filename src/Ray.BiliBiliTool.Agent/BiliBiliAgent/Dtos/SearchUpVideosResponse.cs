using System.Collections.Generic;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class SearchUpVideosResponse
    {
        public UpContent List { get; set; }

        public BiliPageResult Page { get; set; }
    }

    public class UpContent
    {
        public List<UpVideoInfo> Vlist { get; set; }
    }

    public class UpVideoInfo
    {
        public long Aid { get; set; }

        public string Author { get; set; }

        public string Bvid { get; set; }

        public string Title { get; set; }
    }
}
