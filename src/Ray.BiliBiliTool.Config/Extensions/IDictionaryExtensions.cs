using System.Collections.Generic;
using System.Linq;

namespace System.Collections
{
    public static class IDictionaryExtensions
    {
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

        public static Dictionary<string, string> ToDictionary(
            this IDictionary dictionary,
            Func<IEnumerable<DictionaryEntry>, IEnumerable<DictionaryEntry>> otherAction = null)
        {
            return IDictionaryExtensions.ToDictionary(
                dictionary: dictionary,
                keySetter: k => k.ToString(),
                valueSetter: v => v.ToString(),
                otherAction: otherAction);
        }
    }
}