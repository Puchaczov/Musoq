using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public decimal Min([InjectGroup] Group group, string name)
            => Min(group, name, 0);

        [AggregationGetMethod]
        public decimal Min([InjectGroup] Group group, string name, int parent)
        {
            var parentGroup = GetParentGroup(group, parent);
            return parentGroup.GetValue<decimal>(name);
        }

        public void SetMin([InjectGroup] Group group, string name, long? value, int parent = 0)
            => SetMin(group, name, (decimal?)value, parent);

        [AggregationSetMethod]
        public void SetMin([InjectGroup] Group group, string name, decimal? value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!value.HasValue)
            {
                parentGroup.GetOrCreateValue(name, decimal.MaxValue);
                return;
            }

            var storedValue = parentGroup.GetOrCreateValue(name, decimal.MaxValue);

            if (storedValue > value)
                parentGroup.SetValue(name, value);
        }
    }
}
