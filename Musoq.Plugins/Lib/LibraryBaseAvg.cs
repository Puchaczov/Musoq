using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the aggregated average value from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <returns>Aggregated value</returns>
    [AggregationGetMethod]
    public decimal Avg([InjectGroup] Group group, string name)
    {
        return Avg(group, name, 0);
    }

    /// <summary>
    ///     Gets the aggregated average value from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationGetMethod]
    public decimal Avg([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        return Sum(parentGroup, name) / parentGroup.Count;
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, byte? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, sbyte? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, short? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, ushort? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, int? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, uint? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, long? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, ulong? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, float? value, int parent = 0)
    {
        SetSum(group, name, (decimal?)value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, double? value, int parent = 0)
    {
        SetSum(group, name, (decimal?)value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }

    /// <summary>
    ///     Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="value">The value to set</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated value</returns>
    [AggregationSetMethod]
    public void SetAvg([InjectGroup] Group group, string name, decimal? value, int parent = 0)
    {
        SetSum(group, name, value, parent);
        var parentGroup = GetParentGroup(group, parent);
        if (value.HasValue)
            parentGroup.Hit();
    }
}