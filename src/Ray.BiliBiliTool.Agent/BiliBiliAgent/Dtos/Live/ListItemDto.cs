using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class GetListResponse
    {
        public List<LiveSortTag> New_tags { get; set; }

        public List<ListItemDto> List { get; set; }

        public int Has_more { get; set; }
    }

    public class LiveSortTag
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Sort_type { get; set; }
    }

    public class ListItemDto
    {
        public int Roomid { get; set; }

        public int Uid { get; set; }

        public string Title { get; set; }

        public string Uname { get; set; }

        public int Parent_id { get; set; }

        public string Parent_name { get; set; }

        public int Area_id { get; set; }

        public string Area_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <sample>1：百人成就</sample>
        /// <sample>2：天选时刻、新星主播</sample>
        public Dictionary<string, PendantInfo> Pendant_info { get; set; }
    }

    public class PendantInfo
    {
        /// <summary>
        /// Id
        /// </summary>
        /// <sample>504：天选</sample>
        /// <sample>426：百人成就</sample>
        /// <sample>397：新星主播</sample>
        public int Pendent_id { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        /// <sample>天选时刻</sample>
        public string Content { get; set; }
    }
}
