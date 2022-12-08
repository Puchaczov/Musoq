using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        /// <summary>
        /// Gets the min value of a given group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <returns>Min of group</returns>
        [AggregationGetMethod]
        public decimal Min([InjectGroup] Group group, string name)
            => Min(group, name, 0);

        /// <summary>
        /// Gets the min value of a given group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">The name</param>
        /// <param name="parent">The parent</param>
        /// <returns>Min of group</returns>
        [AggregationGetMethod]
        public decimal Min([InjectGroup] Group group, string name, int parent)
        {
            var parentGroup = GetParentGroup(group, parent);
            return parentGroup.GetValue<decimal>(name);
        }

        /// <summary>
        /// Sets the value of the min group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetMin([InjectGroup] Group group, string name, long? value, int parent = 0)
            => SetMin(group, name, (decimal?)value, parent);
        
        /// <summary>
        /// Sets the value of the min group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="value">Value that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
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
