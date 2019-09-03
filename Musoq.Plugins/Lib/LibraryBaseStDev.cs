using Musoq.Plugins.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, decimal? value, int parent = 0)
        {
            SetAvg(group, $"{name}:avgStdDev", value, parent);
            var parentGroup = GetParentGroup(group, parent);

            if (!value.HasValue)
                return;

            var list = parentGroup.GetOrCreateValue($"{name}:itemsStdDev", () => new List<double>());

            list.Add((double)value.Value);
        }

        [AggregationGetMethod]
        public decimal StDev([InjectGroup] Group group, string name)
        {
            var avg = (double)Avg(group, $"{name}:avgStdDev");
            var parentGroup = GetParentGroup(group, 0);
            var values = parentGroup.GetValue<List<double>>($"{name}:itemsStdDev");

            double sum = 0;
            for (int i = 0; i < values.Count; ++i)
            {
                sum += Math.Pow((values[i] - avg), 2);
            }

            var variance = sum / (values.Count - 1);

            return (decimal)Math.Sqrt(variance);
        }
    }
}
