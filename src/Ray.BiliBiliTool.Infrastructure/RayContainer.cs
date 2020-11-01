using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Infrastructure
{
    /// <summary>
    /// 容器
    /// </summary>
    public class RayContainer
    {
        /// <summary>
        /// 根容器
        /// </summary>
        public static IServiceProvider Root { get; set; }
    }
}
