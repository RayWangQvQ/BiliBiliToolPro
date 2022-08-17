using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask
{
    public class ReceiveOrCompleteTaskRequest
    {
        public ReceiveOrCompleteTaskRequest(string taskCode)
        {
            TaskCode=taskCode;
        }

        public string TaskCode { get; set; }
    }
}
