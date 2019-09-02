using Musoq.Plugins.Attributes;
using System;
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

        [BindableMethod]
        public IEnumerable<T> LongestCommonSequence<T>(IEnumerable<T> source, IEnumerable<T> pattern)
            where T : IEquatable<T>
        {
            var sourceCount = source.Count();
            var patternCount = pattern.Count();

            var array = new int[sourceCount, patternCount];
            var maxSubStringSequnce = 0;

            var subSequence = (IEnumerable<T>)null;

            for (int i = 0; i < sourceCount; ++i)
            {
                var sourceElement = source.ElementAt(i);
                for (int j = 0; j < patternCount; ++j)
                {
                    var patternElement = pattern.ElementAt(j);

                    if (sourceElement.Equals(patternElement))
                    {
                        array[i, j] = (i == 0 || j == 0) ? 1 : array[i - 1, j - 1] + 1;

                        if (array[i, j] > maxSubStringSequnce)
                        {
                            maxSubStringSequnce = array[i, j];
                            subSequence = source.Skip(i - maxSubStringSequnce + 1).Take(maxSubStringSequnce);
                        }
                    }
                    else
                    {
                        array[i, j] = 0;
                    }
                }
            }

            if (subSequence == null)
                return Array.Empty<T>();

            return subSequence;
        }

        [AggregationSetMethod]
        public void SetWindow<T>([InjectGroup] Group group, string name, T value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);

            if (value == null)
            {
                parentGroup.GetOrCreateValue(name, new List<T>());
                return;
            }

            var values = parentGroup.GetOrCreateValue(name, () => new List<T>());

            values.Add(value);
        }

        [AggregationGetMethod]
        public IEnumerable<T> Window<T>([InjectGroup] Group group, string name)
        {
            return group.GetValue<List<T>>(name);
        }
    }
}
