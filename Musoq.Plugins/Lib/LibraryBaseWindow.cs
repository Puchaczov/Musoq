using System.Collections.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Creates a window that stores values in a group
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group</param>
    /// <param name="name">The name</param>
    /// <param name="value">The value</param>
    /// <param name="parent">The parent</param>
    /// <typeparam name="T">Type</typeparam>
    [AggregationSetMethod]
    public void SetWindow<T>([InjectGroup] Group group, string name, T value, int parent = 0)
    {
        var parentGroup = GetParentGroup(group, parent);

        if (value == null)
        {
            parentGroup.GetOrCreateValue(name, new List<T>());
            return;
        }

        var values = parentGroup.GetOrCreateValue(name, () => new List<T>());

        values.Add(value);
    }

    /// <summary>
    /// Gets the window
    /// </summary>
    /// <param name="group" injectedByRuntime="true">The group</param>
    /// <param name="name">The name</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Window for that group</returns>
    [AggregationGetMethod]
    public IEnumerable<T> Window<T>([InjectGroup] Group group, string name)
    {
        return group.GetValue<List<T>>(name);
    }
}