using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Video
{
    public class SearchVideosByUpIdDto
    {
        /// <summary>
        /// upId
        /// </summary>
        public long mid { get; set; }

        /// <summary>
        /// pageSize
        /// </summary>
        public int ps { get; set; } = 30;

        /// <summary>
        /// pageNumber
        /// </summary>
        public int pn { get; set; } = 1;

        public int tid { get; set; } = 0;

        public string keyword { get; set; } = "";

        public string order { get; set; } = "pubdate";

        public string platform { get; set; } = "web";

        public int web_location { get; set; } = 1550101;

        public string order_avoided { get; set; } = "true";
    }

    public class SearchVideosByUpIdFullDto: SearchVideosByUpIdDto
    {
        public string w_rid { get; set; } //= "b280d7bc02d653ffa06d874a74e5bfd9"; //todo：应该是md5

        //public long wts { get; set; } = TimeStampHelper.DateTimeToTimeStamp(DateTime.Now);
        public long wts { get; set; } //= 1684857205; //todo
    }
}
