using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace System
{
    public static class ObjectExtensions
    {
        public static string ToJson<T>(this T obj, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                return JsonConvert.SerializeObject(obj);
            }
            else
            {
                return JsonConvert.SerializeObject(obj, settings);
            }
        }

        #region CheckNull

        /// <summary>
        /// 检查参数是否为null，为null时抛出异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">      要检查的对象</param>
        /// <param name="paramName">抛出异常时,显示的参数名</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj" /> 为null时抛出</exception>
        public static void CheckNullWithException<T>(this T obj, string paramName)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// 检查参数是否为null，为null时抛出异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">      要检查的对象</param>
        /// <param name="paramName">抛出异常时,显示的参数名</param>
        /// <param name="message">  抛出异常时,显示的错误信息</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj" /> 为null时抛出</exception>
        public static void CheckNullWithException<T>(this T obj, string paramName, string message)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(paramName, message);
        }

        /// <summary>
        /// 检查参数是否为null或emtpy，为null或emtpy时抛出异常
        /// </summary>
        /// <param name="obj">      要检查的对象</param>
        /// <param name="paramName">抛出异常时,显示的参数名</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj" /> 为null或emtpy时抛出</exception>
        public static void CheckNullOrEmptyWithException(this IEnumerable obj, string paramName)
        {
            if (obj.IsNullOrEmpty()) throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// 检查参数是否为null或emtpy，为null或emtpy时抛出异常
        /// </summary>
        /// <param name="obj">      要检查的对象</param>
        /// <param name="paramName">抛出异常时,显示的参数名</param>
        /// <param name="message">  抛出异常时,显示的错误信息</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj" /> 为null或emtpy时抛出</exception>
        public static void CheckNullOrEmptyWithException(this IEnumerable obj, string paramName, string message)
        {
            if (obj.IsNullOrEmpty()) throw new ArgumentNullException(paramName, message);
        }

        #endregion CheckNull

        #region IsNull and IsNullOrEmpty

        /// <summary>
        /// 判断null，null或0长度都返回true
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要判断的对象</param>
        /// <returns>判断结果,null或0长度返回true,否则返回false</returns>
        public static bool IsNullOrEmpty<T>(this T value)
          where T : class
        {
            #region 1.对象级别

            //引用为null
            bool isObjectNull = value == null;
            if (isObjectNull == true) return true;

            //判断是否为集合
            IEnumerator tempEnumerator = (value as IEnumerable)?.GetEnumerator();
            if (tempEnumerator == null) return false;//这里出去代表是对象 且 引用不为null.所以为false

            #endregion 1.对象级别

            #region 2.集合级别

            //到这里就代表是集合且引用不为空，判断长度
            //MoveNext方法返回tue代表集合中至少有一个数据,返回false就代表0长度
            bool isZeroLenth = tempEnumerator.MoveNext() == false;
            if (isZeroLenth == true) return true;

            return isZeroLenth;

            #endregion 2.集合级别
        }

        /// <summary>
        /// 判断null
        /// </summary>
        /// <param name="value">要判断的对象</param>
        /// <returns>判断结果,null返回true,否则返回false</returns>
        public static bool IsNull(this object value)
        {
            return value == null;
        }

        /// <summary>
        /// 判断null,空数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要判断的数组</param>
        /// <returns>判断结果,null或空数组返回true,否则返回false</returns>
        public static bool IsNullOrEmpty<T>(this T[] value)
        {
            return value == null || value.Length == 0;
        }

        /// <summary>
        /// 判断null,空集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要判断的集合</param>
        /// <returns>判断结果,null或空集合返回true,否则返回false</returns>
        public static bool IsNullOrEmpty<T>(this IList<T> value)
        {
            return value == null || value.Count == 0;
        }

        /// <summary>
        /// 判断null,空字典
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要判断的字典</param>
        /// <returns>判断结果,null或空字典返回true,否则返回false</returns>
        public static bool IsNullOrEmpty<T>(this IDictionary value)
        {
            return value == null || value.Keys.Count == 0;
        }

        /// <summary>
        /// 判断null,空枚举器
        /// </summary>
        /// <param name="value">要判断的字典</param>
        /// <returns>判断结果,null或空枚举器返回true,否则返回false</returns>
        public static bool IsNullOrEmpty(this IEnumerable value)
        {
            return value == null
                || !value.GetEnumerator().MoveNext();
        }

        #endregion IsNull and IsNullOrEmpty

        #region IsNotNull and IsNotNullOrEmpty

        /// <summary>
        /// 判断非null，非0长度
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要判断的对象</param>
        /// <returns>判断结果,非null，非0长度返回true,否则返回false</returns>
        /// <returns></returns>
        public static bool IsNotNullOrEmpty<T>(this T value)
          where T : class
        {
            //IsNullOrEmpty取反
            return !value.IsNullOrEmpty();
        }

        /// <summary>
        /// 判断非null
        /// </summary>
        /// <param name="value">要判断的对象</param>
        /// <returns>判断结果,非null返回true,否则返回false</returns>
        public static bool IsNotNull(this object value)
        {
            return !value.IsNull();
        }

        /// <summary>
        /// 判断非null,非空数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要判断的数组</param>
        /// <returns>判断结果,非null和非空数组返回true,否则为false</returns>
        public static bool IsNotNullOrEmpty<T>(this T[] value)
        {
            return !value.IsNullOrEmpty();
        }

        /// <summary>
        /// 判断非null,非空集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要判断的集合</param>
        /// <returns>判断结果,非null和非空集合返回true,否则为false</returns>
        public static bool IsNotNullOrEmpty<T>(this IList<T> value)
        {
            return !value.IsNullOrEmpty();
        }

        /// <summary>
        /// 判断非null,非空字典
        /// </summary>
        /// <param name="value">要判断的字典</param>
        /// <returns>判断结果,非null和非空字典返回true,否则为false</returns>
        public static bool IsNotNullOrEmpty(this IDictionary value)
        {
            return !value.IsNullOrEmpty();
        }

        /// <summary>
        /// 判断非null,非空枚举器
        /// </summary>
        /// <param name="value">要判断的字典</param>
        /// <returns>判断结果,null或空枚举器返回true,否则返回false</returns>
        public static bool IsNotNullOrEmpty(this IEnumerable value)
        {
            return !value.IsNullOrEmpty();
        }

        #endregion IsNotNull and IsNotNullOrEmpty
    }
}
