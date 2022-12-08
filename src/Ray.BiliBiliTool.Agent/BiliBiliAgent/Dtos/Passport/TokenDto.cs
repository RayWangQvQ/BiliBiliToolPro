using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Passport
{
    public class TokenDto
    {
        public string Url { get; set; }
        public string Refresh_token { get; set; }
        public long Timestamp { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
    }

}
