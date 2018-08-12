using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public decimal Dominant([InjectGroup] Group group, string name, int parent = 0)
        {
            var dict = group.GetValue<SortedDictionary<decimal, Occurence>>(name);

            return dict.First().Key;
        }

        [AggregationSetMethod]
        public void SetDominant([InjectGroup] Group group, string name, long? value, int parent = 0)
            => SetDominant(group, name, (decimal?) value, parent);

        [AggregationSetMethod]
        public void SetDominant([InjectGroup] Group group, string name, decimal? value, int parent = 0)
        {
            if (!value.HasValue)
            {
                group.GetOrCreateValue<decimal>(name);
                return;
            }

            var dict = group.GetOrCreateValue(name, new SortedDictionary<decimal, Occurence>());

            if (!dict.TryGetValue(value.Value, out var occur))
            {
                occur = new Occurence();
                dict.Add(value.Value, occur);
            }

            occur.Increment();
        }
    }
}
