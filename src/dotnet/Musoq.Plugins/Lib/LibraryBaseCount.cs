using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the count value of a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Count of group</returns>
    [AggregationGetMethod]
    public int Count([InjectGroup] Group group, string name)
    {
        return Count(group, name, 0);
    }

    /// <summary>
    ///     Gets the count value of a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Count of group</returns>
    [AggregationGetMethod]
    public int Count([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        return parentGroup.GetValue<int>(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, string? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);

        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, decimal? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);

        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, DateTimeOffset? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, DateTime? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, byte? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, sbyte? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, short? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, ushort? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, int? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, uint? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, long? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, ulong? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, float? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, double? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetCount([InjectGroup] Group group, string name, bool? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (value == null)
        {
            parentGroup.GetOrCreateValue<int>(name);
            return;
        }

        parentGroup.IncrementIntValue(name);
    }
}
