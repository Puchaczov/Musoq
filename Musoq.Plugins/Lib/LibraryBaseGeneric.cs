using Musoq.Plugins.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [BindableMethod]
        public IEnumerable<T> Skip<T>(IEnumerable<T> values, int skipCount)
        {
            return values.Skip(skipCount);
        }

        [BindableMethod]
        public IEnumerable<T> Take<T>(IEnumerable<T> values, int takeCount)
        {
            return values.Take(takeCount);
        }

        [BindableMethod]
        public IEnumerable<T> SkipAndTake<T>(IEnumerable<T> values, int skipCount, int takeCount)
        {
            return values.Skip(skipCount).Take(takeCount);
        }

        [BindableMethod]
        public T[] ToArray<T>(IEnumerable<T> values)
        {
            return values.ToArray();
        }
    }
}
