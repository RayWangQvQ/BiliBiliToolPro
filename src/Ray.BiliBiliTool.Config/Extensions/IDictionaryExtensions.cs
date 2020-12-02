using System.Collections.Generic;
using System.Linq;

namespace System.Collections
{
    public static class IDictionaryExtensions
    {
        /// <summary>
        /// 转换为<see cref="Dictionary{TKey, TValue}"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="keySetter">设置Key的委托</param>
        /// <param name="valueSetter">设置Value的委托</param>
        /// <param name="otherAction">在转换后需要执行的其它委托</param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IDictionary dictionary,
            Func<DictionaryEntry, TKey> keySetter,
            Func<DictionaryEntry, TValue> valueSetter,
            Func<IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>> otherAction = null)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            IEnumerable<KeyValuePair<TKey, TValue>> temp = dictionary
                .Cast<DictionaryEntry>()
                .Select(t => KeyValuePair.Create(keySetter(t), valueSetter(t)));
            temp = otherAction == null ? temp : otherAction(temp);

            return new Dictionary<TKey, TValue>(temp);
        }

        /// <summary>
        /// 转换为<see cref="Dictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="otherAction">在转换后需要执行的其它委托</param>
        /// <returns></returns>
        public static Dictionary<string, string> ToDictionary(
            this IDictionary dictionary,
            Func<IEnumerable<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<string, string>>> otherAction = null)
        {
            return IDictionaryExtensions.ToDictionary(
                dictionary: dictionary,
                keySetter: k => k.Key as string,
                valueSetter: v => v.Value as string,
                otherAction: otherAction);
        }
    }
}
