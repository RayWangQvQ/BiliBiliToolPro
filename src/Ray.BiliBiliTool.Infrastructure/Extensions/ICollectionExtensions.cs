using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public static class ICollectionExtensions
    {
        public static void AddIfNotExist<T>(this ICollection<T> source, T add)
        {
            if (source == null) return;

            if (!source.Any(x=>x.Equals(add))) source.Add(add);
        }

        public static void AddIfNotExist<T>(this ICollection<T> source, T add, Func<T, bool> exist)
        {
            if (source == null) return;

            if (!source.Any(exist)) source.Add(add);
        }
    }
}
