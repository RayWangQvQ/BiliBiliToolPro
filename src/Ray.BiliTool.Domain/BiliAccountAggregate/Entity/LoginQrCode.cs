using Ray.DDD;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliTool.Domain.BiliAccountAggregate.Entity
{
    public class LoginQrCode
    {
        public string CodeKey { get; set; }

        public string CodeInBase64 { get; set; }
    }
}
