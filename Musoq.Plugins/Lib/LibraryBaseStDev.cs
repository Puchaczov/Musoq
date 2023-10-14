using Musoq.Plugins.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        /// <summary>
        /// Gets the StDev value of a given group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="parent">The parent group</param>
        /// <returns>Count of group</returns>
        [AggregationGetMethod]
        public decimal StDev([InjectGroup] Group group, string name, int parent = 0)
        {
            var avg = (double)Avg(group, $"{name}:avgStDev");
            var parentGroup = GetParentGroup(group, parent);
            var values = parentGroup.GetValue<List<double>>($"{name}:itemsStDev");

            var sum = values.Sum(value => Math.Pow(value - avg, 2));

            var variance = sum / values.Count;

            return (decimal)Math.Sqrt(variance);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, byte? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, sbyte? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, short? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, ushort? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, int? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, uint? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, long? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, ulong? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, float? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetStDev([InjectGroup] Group group, string name, double? value, int parent = 0)
            => SetStDev(group, name, (decimal?)value, parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
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
    }
}
