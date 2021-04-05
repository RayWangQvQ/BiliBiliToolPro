using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public static class ICollectionExtensions
    {
        public static void AddIfNotExist<T>(this ICollection<T> source, T add, Func<T, bool> exist)
        {
            if (source == null) return;

            if (source.FirstOrDefault(exist) == null)
                source.Add(add);
        }
    }
}
