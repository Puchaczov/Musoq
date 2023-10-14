﻿using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        /// <summary>
        /// Gets the sum value of a given group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <returns>Sum of group</returns>
        [AggregationGetMethod]
        public decimal Sum([InjectGroup] Group group, string name)
            => Sum(group, name, 0);

        /// <summary>
        /// Gets the sum value of a given group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <returns>Sum of group</returns>
        [AggregationGetMethod]
        public decimal Sum([InjectGroup] Group group, string name, int parent)
        {
            return GetParentGroup(group, parent).GetValue<decimal>(name);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, byte? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, sbyte? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, short? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, ushort? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, int? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, uint? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, long? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, ulong? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        /// <summary>
        /// Sets the value of the group.
        /// </summary>
        /// <param name="group" injectedByRuntime="true">The group object</param>
        /// <param name="name">Name of the group</param>
        /// <param name="number">Number that should be aggregated</param>
        /// <param name="parent">Which group should be used to store value</param>
        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, decimal? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }
    }
}
