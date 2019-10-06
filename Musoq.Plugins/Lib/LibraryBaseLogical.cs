using System;
using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [BindableMethod]
        public T Choose<T>(int index, params T[] values)
        {
            if (values.Length <= index)
                return default;

            return values[index];
        }

        [BindableMethod]
        public T If<T>(bool expresionResult, T a, T b)
        {
            if (expresionResult)
                return a;

            return b;
        }

        [BindableMethod]
        public bool? Match(string regex, string content)
        {
            if (regex == null || content == null)
                return null;

            return Regex.IsMatch(content, regex);
        }

        [BindableMethod]
        public decimal? Coalesce(params decimal?[] array)
            => Coalesce<decimal?>(array);

        [BindableMethod]
        public long? Coalesce(params long?[] array)
            => Coalesce<long?>(array);

        [BindableMethod]
        public T Coalesce<T>(params T[] array)
        {
            foreach (var obj in array)
            {
                if (!Equals(obj, default(T)))
                    return obj;
            }

            return default;
        }
    }
}
