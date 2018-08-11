using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public decimal SumIncome([InjectGroup] Group group, string name)
            => SumIncome(group, name, 0);

        [AggregationGetMethod]
        public decimal SumIncome([InjectGroup] Group group, string name, int parent)
        {
            var parentGroup = GetParentGroup(group, parent);
            return parentGroup.GetRawValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetSumIncome([InjectGroup] Group group, string name, long? number, int parent = 0)
            => SetSumIncome(group, name, (decimal?)number, parent);

        [AggregationSetMethod]
        public void SetSumIncome([InjectGroup] Group group, string name, decimal? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);

            if (number >= 0)
                parentGroup.SetValue(name, value + number);
        }
    }
}
