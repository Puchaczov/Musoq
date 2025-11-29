using System;
using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Adds a given set of time spans.
    /// </summary>
    /// <param name="timeSpans">Time spans that should be added</param>
    /// <returns>Sum of time spans</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.TimeSpan)]
    public TimeSpan? AddTimeSpans(params TimeSpan?[] timeSpans)
    {
        var firstNonNull = timeSpans.Select((value, index) => new {TimeSpan = value, Index = index})
            .FirstOrDefault(pair => pair.TimeSpan.HasValue);
        
        if (firstNonNull == null)
            return null;
        
        var sum = firstNonNull.TimeSpan!.Value;
        
        for (var i = firstNonNull.Index + 1; i < timeSpans.Length; i++)
        {
            if (timeSpans[i].HasValue)
                sum += timeSpans[i]!.Value;
        }
        
        return sum;
    }
    
    /// <summary>
    /// Subtracts a given set of time spans.
    /// </summary>
    /// <param name="timeSpans">Time spans that should be subtracted</param>
    /// <returns>Subtracted time spans</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.TimeSpan)]
    public TimeSpan? SubtractTimeSpans(params TimeSpan?[] timeSpans)
    {
        var firstNonNull = timeSpans.Select((value, index) => new {TimeSpan = value, Index = index})
            .FirstOrDefault(pair => pair.TimeSpan.HasValue);
        
        if (firstNonNull == null)
            return null;
        
        var sum = firstNonNull.TimeSpan!.Value;
        
        for (var i = firstNonNull.Index + 1; i < timeSpans.Length; i++)
        {
            if (timeSpans[i].HasValue)
                sum -= timeSpans[i]!.Value;
        }
        
        return sum;
    }
    
    /// <summary>
    /// Turns a string into a time span.
    /// </summary>
    /// <param name="timeSpan">String that should be converted</param>
    /// <returns>Time span</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.TimeSpan)]
    public TimeSpan? FromString(string timeSpan)
    {
        if (TimeSpan.TryParse(timeSpan, out var result))
            return result;
        
        return null;
    }
    
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