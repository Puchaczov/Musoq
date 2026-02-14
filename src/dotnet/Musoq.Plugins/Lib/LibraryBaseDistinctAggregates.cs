using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

/// <summary>
///     Provides DISTINCT aggregate functions (Count, Sum, Avg, Min, Max with DISTINCT modifier).
///     All use a shared SetDistinctAggregate that stores unique values in a HashSet.
/// </summary>
public partial class LibraryBase
{
    #region Get Methods

    /// <summary>
    ///     Gets the count of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Count of distinct values in the group</returns>
    [AggregationGetMethod]
    public int CountDistinct([InjectGroup] Group group, string name)
    {
        return CountDistinct(group, name, 0);
    }

    /// <summary>
    ///     Gets the count of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Count of distinct values in the group</returns>
    [AggregationGetMethod]
    public int CountDistinct([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetValue<HashSet<object>>(name);
        return hashSet?.Count ?? 0;
    }

    /// <summary>
    ///     Gets the sum of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Sum of distinct values in the group</returns>
    [AggregationGetMethod]
    public decimal SumDistinct([InjectGroup] Group group, string name)
    {
        return SumDistinct(group, name, 0);
    }

    /// <summary>
    ///     Gets the sum of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Sum of distinct values in the group</returns>
    [AggregationGetMethod]
    public decimal SumDistinct([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetValue<HashSet<object>>(name);
        if (hashSet == null || hashSet.Count == 0)
            return 0m;

        return hashSet.Sum(v => Convert.ToDecimal(v));
    }

    /// <summary>
    ///     Gets the average of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Average of distinct values in the group</returns>
    [AggregationGetMethod]
    public decimal AvgDistinct([InjectGroup] Group group, string name)
    {
        return AvgDistinct(group, name, 0);
    }

    /// <summary>
    ///     Gets the average of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Average of distinct values in the group</returns>
    [AggregationGetMethod]
    public decimal AvgDistinct([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetValue<HashSet<object>>(name);
        if (hashSet == null || hashSet.Count == 0)
            return 0m;

        return hashSet.Average(v => Convert.ToDecimal(v));
    }

    /// <summary>
    ///     Gets the minimum of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Minimum of distinct values in the group</returns>
    [AggregationGetMethod]
    public decimal MinDistinct([InjectGroup] Group group, string name)
    {
        return MinDistinct(group, name, 0);
    }

    /// <summary>
    ///     Gets the minimum of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Minimum of distinct values in the group</returns>
    [AggregationGetMethod]
    public decimal MinDistinct([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetValue<HashSet<object>>(name);
        if (hashSet == null || hashSet.Count == 0)
            return 0m;

        return hashSet.Min(v => Convert.ToDecimal(v));
    }

    /// <summary>
    ///     Gets the maximum of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Maximum of distinct values in the group</returns>
    [AggregationGetMethod]
    public decimal MaxDistinct([InjectGroup] Group group, string name)
    {
        return MaxDistinct(group, name, 0);
    }

    /// <summary>
    ///     Gets the maximum of distinct values in a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Maximum of distinct values in the group</returns>
    [AggregationGetMethod]
    public decimal MaxDistinct([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetValue<HashSet<object>>(name);
        if (hashSet == null || hashSet.Count == 0)
            return 0m;

        return hashSet.Max(v => Convert.ToDecimal(v));
    }

    #endregion

    #region Set Methods - Universal for all DISTINCT aggregates

    /// <summary>
    ///     Sets distinct aggregate value for the group (string).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, string? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (decimal).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, decimal? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (DateTimeOffset).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, DateTimeOffset? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (DateTime).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, DateTime? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (byte).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, byte? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (sbyte).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, sbyte? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (short).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, short? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (ushort).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, ushort? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (int).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, int? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (uint).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, uint? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (long).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, long? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (ulong).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, ulong? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (float).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, float? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (double).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, double? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    /// <summary>
    ///     Sets distinct aggregate value for the group (bool).
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetDistinctAggregate([InjectGroup] Group group, string name, bool? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        var hashSet = parentGroup.GetOrCreateValue(name, () => new HashSet<object>());
        
        if (value != null)
            hashSet!.Add(value.Value);
    }

    #endregion
}
