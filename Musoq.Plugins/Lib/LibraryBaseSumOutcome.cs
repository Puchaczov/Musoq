﻿using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public decimal SumOutcome([InjectGroup] Group group, string name, int parent)
        {
            return GetParentGroup(group, parent).GetValue<decimal>(name);
        }

        [AggregationGetMethod]
        public decimal SumOutcome([InjectGroup] Group group, string name)
            => SumOutcome(group, name, 0);

        [AggregationSetMethod]
        public void SetSumOutcome([InjectGroup] Group group, string name, long? number, int parent = 0)
            => SetSumOutcome(group, name, number == null ? (decimal?)null : Convert.ToDecimal(number.Value), parent);

        [AggregationSetMethod]
        public void SetSumOutcome([InjectGroup] Group group, string name, decimal? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);

            if (number < 0)
                parentGroup.SetValue(name, value + number);
        }
    }
}
