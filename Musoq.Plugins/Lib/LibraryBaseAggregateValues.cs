using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Aggregates values into a single value.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <returns>Aggregated values</returns>
    [AggregationGetMethod]
    public string AggregateValues([InjectGroup] Group group, string name)
        => AggregateValues(group, name, 0);

    /// <summary>
    /// Aggregates values into a single value.
    /// </summary>
    /// <param name="group" injectByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="parent">Which group should be used to retrieve value</param>
    /// <returns>Aggregated values</returns>
    [AggregationGetMethod]
    public string AggregateValues([InjectGroup] Group group, string name, int parent)
    {
        var foundedGroup = GetParentGroup(group, parent);
        var list = foundedGroup.GetOrCreateValue<List<string>>(name);

        var builder = new StringBuilder();
        
        if (list == null) return builder.ToString();
        
        for (int i = 0, j = list.Count - 1; i < j; i++)
        {
            builder.Append(list[i]);
            builder.Append(',');
        }

        builder.Append(list[^1]);

        return builder.ToString();
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, string? value, int parent = 0)
    {
        AggregateAdd(group, name, value ?? string.Empty, parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, byte? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, sbyte? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, short? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, ushort? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, int? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, uint? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, long? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, ulong? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, float? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, double? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, decimal? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, DateTimeOffset? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString("dd.MM.yyyy HH:mm:ss zzz", CultureInfo.CurrentCulture), parent);
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="culture">What culture the object will be represented at</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, DateTimeOffset? value, string culture, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString("dd.MM.yyyy HH:mm:ss zzz", CultureInfo.GetCultureInfo(culture)), parent);
    }
        
    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, DateTime? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture), parent);
    }
        
    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, char? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }
        
    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">Name of the group</param>
    /// <param name="value">Value that should be aggregated</param>
    /// <param name="parent">Which group should be used to store value</param>
    [AggregationSetMethod]
    public void SetAggregateValues([InjectGroup] Group group, string name, bool? value, int parent = 0)
    {
        if (!value.HasValue)
        {
            AggregateAdd(group, name, string.Empty, parent);
            return;
        }

        AggregateAdd(group, name, value.Value.ToString(CultureInfo.CurrentCulture), parent);
    }

    private static void AggregateAdd<TType>(Group group, string name, TType value, int parent)
    {
        var foundGroup = GetParentGroup(group, parent);
        
        var list = foundGroup.GetOrCreateValue(name, new List<TType>());
        
        if (list == null)
            throw new InvalidOperationException($"Group list must not be null. Group name: {name}");
        
        list.Add(value);
    }
}