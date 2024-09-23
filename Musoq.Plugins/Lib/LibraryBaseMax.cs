using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Gets the max value of a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Max of group</returns>
    [AggregationGetMethod]
    public decimal Max([InjectGroup] Group group, string name)
        => Max(group, name, 0);

    /// <summary>
    /// Gets the max value of a given group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Max of group</returns>
    [AggregationGetMethod]
    public decimal Max([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        return parentGroup.GetValue<decimal>(name);
    }

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, byte? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, sbyte? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, short? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, ushort? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, int? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, uint? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, long? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, ulong? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, float? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, double? value, int parent = 0)
        => SetMax(group, name, (decimal?) value, parent);

    /// <summary>
    /// Sets the value of the max group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetMax([InjectGroup] Group group, string name, decimal? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);
        if (!value.HasValue)
        {
            parentGroup.GetOrCreateValue(name, decimal.MinValue);
            return;
        }

        var storedValue = parentGroup.GetOrCreateValue(name, decimal.MinValue);

        if (storedValue < value)
            parentGroup.SetValue(name, value);
    }
}