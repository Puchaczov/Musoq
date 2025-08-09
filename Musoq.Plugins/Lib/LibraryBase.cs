using System;
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
    /// Gets the rank of the current row (same as RowNumber for now).
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <returns>The rank</returns>
    [BindableMethod]
    public int Rank([InjectQueryStats] QueryStats info)
    {
        return info.RowNumber;
    }

    /// <summary>
    /// Gets the dense rank of the current row (same as RowNumber for now).
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <returns>The dense rank</returns>
    [BindableMethod]
    public int DenseRank([InjectQueryStats] QueryStats info)
    {
        return info.RowNumber;
    }

    /// <summary>
    /// Placeholder for LAG function - returns null for now.
    /// In a full implementation, this would access values from previous rows.
    /// </summary>
    /// <typeparam name="T">The type of value to lag</typeparam>
    /// <param name="value">The value to lag</param>
    /// <param name="offset">Number of rows to lag (default 1)</param>
    /// <param name="defaultValue">Default value if no lagged value available</param>
    /// <returns>The lagged value or default</returns>
    [BindableMethod]
    public T? Lag<T>(T value, int offset = 1, T? defaultValue = default)
    {
        // Basic implementation - just return default value
        // A full implementation would need access to the complete result set
        return defaultValue;
    }

    /// <summary>
    /// Placeholder for LEAD function - returns null for now.
    /// In a full implementation, this would access values from next rows.
    /// </summary>
    /// <typeparam name="T">The type of value to lead</typeparam>
    /// <param name="value">The value to lead</param>
    /// <param name="offset">Number of rows to lead (default 1)</param>
    /// <param name="defaultValue">Default value if no lead value available</param>
    /// <returns>The lead value or default</returns>
    [BindableMethod]
    public T? Lead<T>(T value, int offset = 1, T? defaultValue = default)
    {
        // Basic implementation - just return default value
        // A full implementation would need access to the complete result set
        return defaultValue;
    }

    /// <summary>
    /// Gets the sum of values over a window (aggregate window function).
    /// </summary>
    /// <param name="value">The value to sum</param>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The sum over the window</returns>
    [BindableMethod]
    public T Sum<T>(T value, [InjectQueryStats] QueryStats info) where T : struct
    {
        return value; // Basic implementation - will be enhanced with true windowing
    }

    /// <summary>
    /// Gets the count of values over a window (aggregate window function).
    /// </summary>
    /// <param name="value">The value to count</param>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The count over the window</returns>
    [BindableMethod]
    public long Count<T>(T value, [InjectQueryStats] QueryStats info)
    {
        return info.RowNumber; // Basic implementation - will be enhanced with true windowing
    }

    /// <summary>
    /// Gets the average of values over a window (aggregate window function).
    /// </summary>
    /// <param name="value">The value to average</param>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The average over the window</returns>
    [BindableMethod]
    public double Avg<T>(T value, [InjectQueryStats] QueryStats info) where T : struct
    {
        return Convert.ToDouble(value); // Basic implementation - will be enhanced with true windowing
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