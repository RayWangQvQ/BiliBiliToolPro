using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{

    public class GetArteaListResponse
    {
        public List<AreaDto> Data { get; set; }
    }

    public class AreaDto
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
