using Musoq.Plugins.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, double? value, int parent = 0)
            => SetStDev(group, name, value ?? null, parent);

        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, decimal? value, int parent = 0)
        {
            SetAvg(group, $"{name}:avgStDev", value, parent);
            var parentGroup = GetParentGroup(group, parent);

            if (!value.HasValue)
                return;

            var list = parentGroup.GetOrCreateValue($"{name}:itemsStDev", () => new List<double>());

            list.Add((double)value.Value);
        }

        [AggregationGetMethod]
        public decimal StDev([InjectGroup] Group group, string name, int parent = 0)
        {
            var avg = (double)Avg(group, $"{name}:avgStDev");
            var parentGroup = GetParentGroup(group, parent);
            var values = parentGroup.GetValue<List<double>>($"{name}:itemsStDev");

            double sum = 0;
            for (int i = 0; i < values.Count; ++i)
            {
                sum += Math.Pow((values[i] - avg), 2);
            }

            var variance = sum / values.Count;

            return (decimal)Math.Sqrt(variance);
        }
    }
}
