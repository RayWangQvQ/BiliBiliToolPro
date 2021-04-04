using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation
{
    public class TagDto
    {
        public int Tagid { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// 关注up个数
        /// </summary>
        public int Count { get; set; }

        public string Tip { get; set; }
    }
}
