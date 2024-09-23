using System.Diagnostics.CodeAnalysis;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

/// <summary>
/// Library base type that all other types should inherit from.
/// </summary>
[BindableClass]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public partial class LibraryBase : UserMethodsLibrary
{
    /// <summary>
    /// Gets the row number of the current row.
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <returns>The row number</returns>
    [BindableMethod]
    public int RowNumber([InjectQueryStats] QueryStats info)
    {
        return info.RowNumber;
    }

    /// <summary>
    /// Gets the typename of passed object.
    /// </summary>
    /// <param name="obj">Object of unknown type that the typename have to be retrieved</param>
    /// <returns>The typename value</returns>
    [BindableMethod]
    public string? GetTypeName(object? obj)
    {
        return obj?.GetType().FullName;
    }
}