using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.DomainService.Dtos
{
    public class VideoInfoDto
    {
        public string Aid { get; set; }

        public string Bvid { get; set; }

        public long Cid { get; set; }

        public string Title { get; set; }

        public int? Duration { get; set; }

        /// <summary>
        /// 是否转载
        /// <sample>1：原创</sample>
        /// <sample>2：转载</sample>
        /// </summary>
        public int Copyright { get; set; }
    }
}
