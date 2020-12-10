using System.Collections.Generic;

namespace System.Collections
{
    public static class KeyValuePairExtensions
    {
        /// <summary>
        /// 获得一个新的<see cref="KeyValuePair{TNewKey, TNewValue}"/>
        /// </summary>
        /// <typeparam name="TKey">旧Key类型</typeparam>
        /// <typeparam name="TValue">旧Value类型</typeparam>
        /// <typeparam name="TNewKey">新Key类型</typeparam>
        /// <typeparam name="TNewValue">新Value类型</typeparam>
        /// <param name="oldKv"></param>
        /// <param name="key">设置新Key的委托</param>
        /// <param name="value">设置新Value的委托</param>
        /// <returns></returns>
        public static KeyValuePair<TNewKey, TNewValue> New<TKey, TValue, TNewKey, TNewValue>(
            this KeyValuePair<TKey, TValue> oldKv,
            Func<TKey, TNewKey> key,
            Func<TValue, TNewValue> value)
        {
            return KeyValuePair.Create(key(oldKv.Key), value(oldKv.Value));
        }

        /// <summary>
        /// 获得一个新的<see cref="KeyValuePair{TNewKey, TValue}"/>
        /// </summary>
        /// <typeparam name="TKey">旧Key类型</typeparam>
        /// <typeparam name="TValue">旧Value类型</typeparam>
        /// <typeparam name="TNewKey">新Key类型</typeparam>
        /// <param name="oldKv"></param>
        /// <param name="key">设置新Key的委托</param>
        /// <returns></returns>
        public static KeyValuePair<TNewKey, TValue> NewKey<TKey, TValue, TNewKey>(
           this KeyValuePair<TKey, TValue> oldKv,
           Func<TKey, TNewKey> key)
        {
            return KeyValuePairExtensions.New(oldKv, key, t => t);
        }

        /// <summary>
        /// 获得一个新的<see cref="KeyValuePair{TNewKey, TValue}"/>
        /// </summary>
        /// <typeparam name="TKey">旧Key类型</typeparam>
        /// <typeparam name="TValue">旧Value类型</typeparam>
        /// <typeparam name="TNewKey">新Key类型</typeparam>
        /// <param name="oldKv"></param>
        /// <param name="key">新Key</param>
        /// <returns></returns>
        public static KeyValuePair<TNewKey, TValue> NewKey<TKey, TValue, TNewKey>(
           this KeyValuePair<TKey, TValue> oldKv,
           TNewKey key)
        {
            return KeyValuePairExtensions.New(oldKv, t => key, t => t);
        }

        /// <summary>
        /// 获得一个新的<see cref="KeyValuePair{TKey, TNewValue}"/>
        /// </summary>
        /// <typeparam name="TKey">旧Key类型</typeparam>
        /// <typeparam name="TValue">旧Value类型</typeparam>
        /// <typeparam name="TNewValue">新Value类型</typeparam>
        /// <param name="oldKv"></param>
        /// <param name="value">设置新Value的委托</param>
        /// <returns></returns>
        public static KeyValuePair<TKey, TNewValue> NewValue<TKey, TValue, TNewValue>(
           this KeyValuePair<TKey, TValue> oldKv,
           Func<TValue, TNewValue> value)
        {
            return KeyValuePairExtensions.New(oldKv, t => t, value);
        }

        /// <summary>
        /// 获得一个新的<see cref="KeyValuePair{TKey, TNewValue}"/>
        /// </summary>
        /// <typeparam name="TKey">旧Key类型</typeparam>
        /// <typeparam name="TValue">旧Value类型</typeparam>
        /// <typeparam name="TNewValue">新Value类型</typeparam>
        /// <param name="oldKv"></param>
        /// <param name="value">新Value/param>
        /// <returns></returns>
        public static KeyValuePair<TKey, TNewValue> NewValue<TKey, TValue, TNewValue>(
           this KeyValuePair<TKey, TValue> oldKv,
           TNewValue value)
        {
            return KeyValuePairExtensions.New(oldKv, t => t, t => value);
        }
    }
}
