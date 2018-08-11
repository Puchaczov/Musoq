using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public string AggregateValues([InjectGroup] Group group, string name)
            => AggregateValues(group, name, 0);

        [AggregationGetMethod]
        public string AggregateValues([InjectGroup] Group group, string name, long number)
        {
            var foundedGroup = GetParentGroup(group, number);
            var list = foundedGroup.GetOrCreateValue<List<string>>(name);

            var builder = new StringBuilder();
            for (int i = 0, j = list.Count - 1; i < j; i++)
            {
                builder.Append(list[i]);
                builder.Append(',');
            }

            builder.Append(list[list.Count - 1]);

            return builder.ToString();
        }

        [AggregationSetMethod]
        public void SetAggregateValues([InjectGroup] Group group, string name, string value, long parent = 0)
        {
            AggregateAdd(group, name, value ?? string.Empty, parent);
        }

        [AggregationSetMethod]
        public void SetAggregateValues([InjectGroup] Group group, string name, decimal? value, long parent = 0)
        {
            if (!value.HasValue)
            {
                AggregateAdd(group, name, string.Empty, parent);
                return;
            }

            AggregateAdd(group, name, value.Value.ToString(CultureInfo.InvariantCulture), parent);
        }

        [AggregationSetMethod]
        public void SetAggregateValues([InjectGroup] Group group, string name, long? value, long parent = 0)
        {
            if (!value.HasValue)
            {
                AggregateAdd(group, name, string.Empty, parent);
                return;
            }

            AggregateAdd(group, name, value.Value.ToString(CultureInfo.InvariantCulture), parent);
        }

        [AggregationSetMethod]
        public void SetAggregateValues([InjectGroup] Group group, string name, DateTimeOffset? value, long parent = 0)
        {
            if (!value.HasValue)
            {
                AggregateAdd(group, name, string.Empty, parent);
                return;
            }

            AggregateAdd(group, name, value.Value.ToString(CultureInfo.InvariantCulture), parent);
        }

        [AggregationSetMethod]
        public void SetAggregateValues([InjectGroup] Group group, string name, DateTime? value, long parent = 0)
        {
            if (!value.HasValue)
            {
                AggregateAdd(group, name, string.Empty, parent);
                return;
            }

            AggregateAdd(group, name, value.Value.ToString(CultureInfo.InvariantCulture), parent);
        }

        private static void AggregateAdd<TType>(Group group, string name, TType value, long parent)
        {
            var foundedGroup = GetParentGroup(group, parent);
            var list = foundedGroup.GetOrCreateValue(name, new List<TType>());
            list.Add(value);
        }
    }
}
