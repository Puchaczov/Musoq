using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        /// <summary>
        /// Gets the outcome value of a given group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <returns>Outcome of group</returns>
        [AggregationGetMethod]
        public decimal SumOutcome([InjectGroup] Group group, string name)
            => SumOutcome(group, name, 0);
        
        /// <summary>
        /// Gets the outcome value of a given group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="parent">Which group should be used to retrieve value</param>
        /// <returns>Outcome of group</returns>
        [AggregationGetMethod]
        public decimal SumOutcome([InjectGroup] Group group, string name, int parent)
        {
            return GetParentGroup(group, parent).GetValue<decimal>(name);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSumOutcome([InjectGroup] Group group, string name, long? number, int parent = 0)
            => SetSumOutcome(group, name, number == null ? (decimal?)null : Convert.ToDecimal(number.Value), parent);

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
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
