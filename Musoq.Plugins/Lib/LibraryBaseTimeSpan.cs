using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Gets the sum value of a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Sum of group</returns>
    [AggregationGetMethod]
    public TimeSpan? SumTimeSpan([InjectGroup] Group group, string name)
        => SumTimeSpan(group, name, 0);

    /// <summary>
    /// Gets the sum value of a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Sum of group</returns>
    [AggregationGetMethod]
    public TimeSpan? SumTimeSpan([InjectGroup] Group group, string name, int parent)
    {
        return GetParentGroup(group, parent).GetValue<TimeSpan>(name);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="timeSpan">TimeSpan that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetSumTimeSpan([InjectGroup] Group group, string name, TimeSpan? timeSpan, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (!timeSpan.HasValue)
        {
            parentGroup.GetOrCreateValue<TimeSpan>(name);
            return;
        }

        var value = parentGroup.GetOrCreateValue<TimeSpan>(name);
        parentGroup.SetValue(name, value + timeSpan);
    }
    
    /// <summary>
    /// Gets the min value of a given group.
    /// </summary>
    /// <param name="group">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Min of group</returns>
    [AggregationGetMethod]
    public TimeSpan? MinTimeSpan([InjectGroup] Group group, string name)
        => MinTimeSpan(group, name, 0);
    
    /// <summary>
    /// Gets the min value of a given group.
    /// </summary>
    /// <param name="group">The group object</param>
    /// <param name="name">The name</param>
    /// <param name="parent">The parent</param>
    /// <returns>Min of group</returns>
    [AggregationGetMethod]
    public TimeSpan? MinTimeSpan([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        return parentGroup.GetValue<TimeSpan>(name);
    }
    
    /// <summary>
    /// Sets the value of the min group.
    /// </summary>
    /// <param name="group">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="timeSpan">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMinTimeSpan([InjectGroup] Group group, string name, TimeSpan? timeSpan, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (!timeSpan.HasValue)
        {
            parentGroup.GetOrCreateValue(name, TimeSpan.MaxValue);
            return;
        }

        var storedValue = parentGroup.GetOrCreateValue(name, TimeSpan.MaxValue);

        if (storedValue > timeSpan)
            parentGroup.SetValue(name, timeSpan);
    }
    
    /// <summary>
    /// Gets the max value of a given group.
    /// </summary>
    /// <param name="group">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Max of group</returns>
    [AggregationGetMethod]
    public TimeSpan? MaxTimeSpan([InjectGroup] Group group, string name)
        => MinTimeSpan(group, name, 0);
    
    /// <summary>
    /// Gets the max value of a given group.
    /// </summary>
    /// <param name="group">The group object</param>
    /// <param name="name">The name</param>
    /// <param name="parent">The parent</param>
    /// <returns>Max of group</returns>
    [AggregationGetMethod]
    public TimeSpan? MaxTimeSpan([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        return parentGroup.GetValue<TimeSpan>(name);
    }
    
    /// <summary>
    /// Sets the value of the min group.
    /// </summary>
    /// <param name="group">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="timeSpan">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMaxTimeSpan([InjectGroup] Group group, string name, TimeSpan? timeSpan, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (!timeSpan.HasValue)
        {
            parentGroup.GetOrCreateValue(name, TimeSpan.MinValue);
            return;
        }

        var storedValue = parentGroup.GetOrCreateValue(name, TimeSpan.MinValue);

        if (storedValue < timeSpan)
            parentGroup.SetValue(name, timeSpan);
    }
}