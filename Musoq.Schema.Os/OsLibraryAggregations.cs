using System.Collections.Generic;
using System.IO;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Os.Files;

namespace Musoq.Schema.Os;

public partial class OsLibrary
{
    /// <summary>
    /// Gets the aggregated average value from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <returns>Aggregated value</returns>
    [AggregationGetMethod]
    public IReadOnlyList<ExtendedFileInfo> AggregateFiles([InjectGroup] Group group, string name)
    {
        return group.GetValue<IReadOnlyList<ExtendedFileInfo>>(name);
    }

    /// <summary>
    /// Gets the aggregated average value from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <returns>Aggregated value</returns>
    [AggregationGetMethod]
    public IReadOnlyList<DirectoryInfo> AggregateDirectories([InjectGroup] Group group, string name)
    {
        return group.GetValue<IReadOnlyList<DirectoryInfo>>(name);
    }

    /// <summary>
    /// Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="file">The value to set</param>
    /// <returns>Aggregated value</returns>    
    [AggregationSetMethod]
    public void SetAggregateFiles([InjectGroup] Group group, string name, ExtendedFileInfo file)
    {
        var list = group.GetOrCreateValue(name, new List<ExtendedFileInfo>());

        list.Add(file);
    }

    /// <summary>
    /// Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="name">The name of the group</param>
    /// <param name="file">The value to set</param>
    /// <returns>Aggregated value</returns>   
    [AggregationSetMethod]
    public void SetAggregateFiles([InjectGroup] Group group, [InjectSource] ExtendedFileInfo file, string name)
    {
        var list = group.GetOrCreateValue(name, new List<ExtendedFileInfo>());

        list.Add(file);
    }

    /// <summary>
    /// Sets the value to average aggregation from the given group name
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group object</param>
    /// <param name="directory">The value to set</param>
    /// <param name="name">The name of the group</param>
    /// <returns>Aggregated value</returns>   
    [AggregationSetMethod]
    public void SetAggregateDirectories([InjectGroup] Group group, [InjectSource] DirectoryInfo directory, string name)
    {
        var list = group.GetOrCreateValue(name, new List<DirectoryInfo>());

        list.Add(directory);
    }
}