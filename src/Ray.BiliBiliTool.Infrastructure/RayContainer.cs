using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

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

        #region DI相关

        private static Func<Type, object> getObjectFromDi;
        private static Type loggerType = typeof(ILogger<>);

        public static void SetGetServiceFunc(Func<Type, object> func)
        {
            getObjectFromDi = func;
        }

        public static ILogger<T> GetLogger<T>()
        {
            if (RayContainer.getObjectFromDi == null)
            {
                throw new SystemException($"{nameof(RayContainer.getObjectFromDi)} 未初始化");
            }

            Type type = typeof(T);
            Type newType = loggerType.MakeGenericType(type);

            return RayContainer.getObjectFromDi(newType) as ILogger<T>;
        }

        #endregion DI相关
    }
}
