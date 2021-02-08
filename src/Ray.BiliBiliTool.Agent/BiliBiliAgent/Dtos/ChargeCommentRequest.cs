using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class ChargeCommentRequest
    {
        public ChargeCommentRequest(string order_id, string message, string csrf)
        {
            Order_id = order_id;
            Message = message;
            Csrf = csrf;
        }

        public string Order_id { get; set; }

        public string Message { get; set; }

        public string Csrf { get; set; }
    }
}
