using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public decimal Sum([InjectGroup] Group group, string name)
        {
            return group.GetValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, decimal? number)
        {
            if (!number.HasValue)
            {
                group.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = group.GetOrCreateValue<decimal>(name);
            group.SetValue(name, value + number);
        }

        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, long? number)
        {
            if (!number.HasValue)
            {
                group.GetOrCreateValue<long>(name);
                return;
            }

            var value = group.GetOrCreateValue<long>(name);
            group.SetValue(name, value + number);
        }
    }
}
