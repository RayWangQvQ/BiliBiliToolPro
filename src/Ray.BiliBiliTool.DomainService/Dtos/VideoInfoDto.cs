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

        public string Title { get; set; }

        public int? SecondsLength { get; set; }
    }
}
