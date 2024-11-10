using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Converts the given value to DateTimeOffset using the current culture.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted DateTimeOffset value, or null if the conversion fails.</returns>
    [BindableMethod]
    public DateTimeOffset? ToDateTimeOffset(string value) => ToDateTimeOffset(value, CultureInfo.CurrentCulture.Name);

    /// <summary>
    /// Converts the given value to DateTimeOffset using the specified culture.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>The converted DateTimeOffset value, or null if the conversion fails.</returns>
    [BindableMethod]
    public DateTimeOffset? ToDateTimeOffset(string value, string culture)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!DateTimeOffset.TryParse(value, CultureInfo.GetCultureInfo(culture), DateTimeStyles.None, out var result))
            return null;

        return result;
    }
    
    /// <summary>
    /// Subtracts the second DateTimeOffset from the first DateTimeOffset.
    /// </summary>
    /// <param name="first">The first DateTimeOffset.</param>
    /// <param name="second">The second DateTimeOffset.</param>
    /// <returns>The result of the subtraction, or null if either of the values is null.</returns>
    [BindableMethod]
    public TimeSpan? SubtractDateTimeOffsets(DateTimeOffset? first, DateTimeOffset? second)
    {
        if (first is null || second is null)
            return null;

        return first.Value - second.Value;
    }

    /// <summary>
    /// Retrieves the maximum DateTimeOffset value from the specified group.
    /// </summary>
    /// <param name="group">The group to retrieve the value from.</param>
    /// <param name="name">The name of the value to retrieve.</param>
    /// <param name="parent">The parent group index.</param>
    /// <returns>The maximum DateTimeOffset value.</returns>
    [AggregationGetMethod]
    public DateTimeOffset? MaxDateTimeOffset([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        return parentGroup.GetValue<DateTimeOffset?>(name);
    }

    /// <summary>
    /// Retrieves the maximum DateTimeOffset value from the specified group.
    /// </summary>
    /// <param name="group">The group to retrieve the value from.</param>
    /// <param name="name">The name of the value to retrieve.</param>
    /// <returns>The maximum DateTimeOffset value.</returns>
    [AggregationGetMethod]
    public DateTimeOffset? MaxDateTimeOffset([InjectGroup] Group group, string name)
    {
        var parentGroup = GetParentGroup(group, 0);
        return parentGroup.GetValue<DateTimeOffset?>(name);
    }

    /// <summary>
    /// Sets the maximum DateTimeOffset value in the specified group.
    /// </summary>
    /// <param name="group">The group to set the value in.</param>
    /// <param name="name">The name of the value to set.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="parent">The parent group index.</param>
    [AggregationSetMethod]
    public void SetMaxDateTimeOffset([InjectGroup] Group group, string name, DateTimeOffset? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);

        if (value == null)
        {
            parentGroup.GetOrCreateValue<DateTimeOffset?>(name, () => null);
            return;
        }

        var maxDateTimeOffset = parentGroup.GetOrCreateValue<DateTimeOffset?>(name, DateTimeOffset.MinValue);
        
        if (maxDateTimeOffset is null)
            parentGroup.SetValue(name, value.Value);

        if (maxDateTimeOffset < value)
            parentGroup.SetValue(name, value.Value);
    }

    /// <summary>
    /// Retrieves the minimum DateTimeOffset value from the specified group.
    /// </summary>
    /// <param name="group">The group to retrieve the value from.</param>
    /// <param name="name">The name of the value to retrieve.</param>
    /// <param name="parent">The parent group index.</param>
    /// <returns>The minimum DateTimeOffset value.</returns>
    [AggregationGetMethod]
    public DateTimeOffset? MinDateTimeOffset([InjectGroup] Group group, string name, int parent)
    {
        var parentGroup = GetParentGroup(group, parent);
        return parentGroup.GetValue<DateTimeOffset?>(name);
    }

    /// <summary>
    /// Retrieves the minimum DateTimeOffset value from the specified group.
    /// </summary>
    /// <param name="group">The group to retrieve the value from.</param>
    /// <param name="name">The name of the value to retrieve.</param>
    /// <returns>The minimum DateTimeOffset value.</returns>
    [AggregationGetMethod]
    public DateTimeOffset? MinDateTimeOffset([InjectGroup] Group group, string name)
    {
        var parentGroup = GetParentGroup(group, 0);
        return parentGroup.GetValue<DateTimeOffset?>(name);
    }

    /// <summary>
    /// Sets the minimum DateTimeOffset value in the specified group.
    /// </summary>
    /// <param name="group">The group to set the value in.</param>
    /// <param name="name">The name of the value to set.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="parent">The parent group index.</param>
    [AggregationSetMethod]
    public void SetMinDateTimeOffset([InjectGroup] Group group, string name, DateTimeOffset? value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);

        if (value == null)
        {
            parentGroup.GetOrCreateValue<DateTimeOffset?>(name, () => null);
            return;
        }

        var minDateTimeOffset = parentGroup.GetOrCreateValue<DateTimeOffset?>(name, DateTimeOffset.MaxValue);
        
        if (minDateTimeOffset is null)
            parentGroup.SetValue(name, value.Value);

        if (minDateTimeOffset > value)
            parentGroup.SetValue(name, value.Value);
    }
}