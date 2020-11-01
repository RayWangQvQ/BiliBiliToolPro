using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure
{
    /// <summary>
    /// 配置
    /// </summary>
    public class RayConfiguration
    {
        /// <summary>
        /// 根配置
        /// </summary>
        public static IConfigurationRoot Root { get; set; }
    }
}
