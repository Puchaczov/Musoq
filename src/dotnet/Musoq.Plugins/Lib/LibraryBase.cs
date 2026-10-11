using System.Diagnostics.CodeAnalysis;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

/// <summary>
///     Library base type that all other types should inherit from.
/// </summary>
[BindableClass]
[LibraryExecutionDefaults(
    AggregatePermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups,
    WindowPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentPartitions)]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public partial class LibraryBase
{
    /// <summary>
    ///     Gets the row number of the current row.
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <returns>The row number</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public int RowNumber([InjectQueryStats] QueryStats info)
    {
        ArgumentNullException.ThrowIfNull(info);
        return info.RowNumber;
    }

    /// <summary>
    ///     Gets the row number of the current row (overload for struct-based stats to avoid allocations).
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <returns>The row number</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public int RowNumber([InjectQueryStats] IQueryStats info)
    {
        ArgumentNullException.ThrowIfNull(info);
        return info.RowNumber;
    }

    /// <summary>
    ///     Gets the typename of passed object.
    /// </summary>
    /// <param name="obj">Object of unknown type that the typename have to be retrieved</param>
    /// <returns>The typename value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public string? GetTypeName(object? obj)
    {
        return obj?.GetType().FullName;
    }
    /// <summary>Returns the concrete type name without boxing a value-type argument.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public string? GetTypeName<T>(T value)
    {
        if (typeof(T).IsValueType)
            return typeof(T).FullName;

        if (value is null)
            return null;

        return value.GetType().FullName;
    }

    /// <summary>Returns the underlying type name for a present nullable value.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public string? GetTypeName<T>(T? value) where T : struct
        => value.HasValue ? typeof(T).FullName : null;

}
