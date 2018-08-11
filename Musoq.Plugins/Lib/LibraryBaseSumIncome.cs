using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {

        [AggregationGetMethod]
        public decimal SumIncome([InjectGroup] Group group, string name, long number)
        {
            var parent = GetParentGroup(group, number);
            var value = parent.GetRawValue<decimal>(name);
            return value;
        }

        [AggregationGetMethod]
        public decimal SumIncome([InjectGroup] Group group, string name)
        {
            return group.GetValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetSumIncome([InjectGroup] Group group, string name, long number, decimal? value)
        {
            var parent = GetParentGroup(group, number);
            SetSumIncome(parent, name, value);
            group.GetOrCreateValueWithConverter<Group, decimal>(name, parent,
                o => ((Group)o).GetRawValue<decimal>(name));
        }

        [AggregationSetMethod]
        public void SetSumIncome([InjectGroup] Group group, string name, decimal? number)
        {
            if (!number.HasValue)
            {
                group.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = group.GetOrCreateValue<decimal>(name);

            if (number >= 0)
                group.SetValue(name, value + number);
        }
    }
}
