using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation
{
    public class CreateTagRequest
    {
        public string Tag { get; set; }

        public string Csrf { get; set; }
    }
}
