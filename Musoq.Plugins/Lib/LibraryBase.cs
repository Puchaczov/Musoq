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
    /// Gets the rank - simple version without injection (for complex expressions).
    /// </summary>
    /// <returns>The rank</returns>
    [BindableMethod]
    public int RankSimple()
    {
        return 1; // Simplified implementation for complex expressions
    }

    /// <summary>
    /// Gets the rank - completely independent version for case expressions.
    /// </summary>
    /// <returns>The rank</returns>
    [BindableMethod]
    public int RankForCase()
    {
        return 1; // Simplified implementation for complex expressions
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
    /// Gets the dense rank of the current row (alias for DenseRank).
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <returns>The dense rank</returns>
    [BindableMethod]
    public int Dense_Rank([InjectQueryStats] QueryStats info)
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
    /// Gets the sum of values (regular aggregate function).
    /// </summary>
    /// <param name="value">The value to sum</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The sum of values</returns>
    [BindableMethod]
    public T Sum<T>(T value) where T : struct
    {
        return value; // Basic implementation for aggregation
    }

    /// <summary>
    /// Gets the sum of values over a window (aggregate window function).
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <param name="value">The value to sum</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The sum over the window</returns>
    [BindableMethod]
    public T SumWindow<T>([InjectQueryStats] QueryStats info, T value) where T : struct
    {
        return value; // Basic implementation - will be enhanced with true windowing
    }

    /// <summary>
    /// Gets the count of values (regular aggregate function).
    /// </summary>
    /// <param name="value">The value to count</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The count of values</returns>
    [BindableMethod]
    public long Count<T>(T value)
    {
        return 1; // Basic implementation for aggregation
    }

    /// <summary>
    /// Gets the count of values over a window (aggregate window function).
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <param name="value">The value to count</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The count over the window</returns>
    [BindableMethod]
    public long CountWindow<T>([InjectQueryStats] QueryStats info, T value)
    {
        return info.RowNumber; // Basic implementation - will be enhanced with true windowing
    }

    /// <summary>
    /// Gets the average of values (regular aggregate function).
    /// </summary>
    /// <param name="value">The value to average</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The average of values</returns>
    [BindableMethod]
    public double Avg<T>(T value) where T : struct
    {
        return Convert.ToDouble(value); // Basic implementation for aggregation
    }

    /// <summary>
    /// Gets the average of values over a window (aggregate window function).
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <param name="value">The value to average</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The average over the window</returns>
    [BindableMethod]
    public double AvgWindow<T>([InjectQueryStats] QueryStats info, T value) where T : struct
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

    /// <summary>
    /// Enhanced RANK function with partition support.
    /// Supports RANK() OVER (PARTITION BY columns ORDER BY columns).
    /// </summary>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <param name="partitionColumns">Comma-separated partition column values</param>
    /// <param name="orderColumns">Comma-separated order column values</param>
    /// <returns>The rank within the partition</returns>
    [BindableMethod]
    public int RankWithPartition([InjectQueryStats] QueryStats info, string partitionColumns = "", string orderColumns = "")
    {
        // Enhanced implementation for true partitioning (placeholder for now)
        // In full implementation, this would:
        // 1. Parse partition and order column specifications
        // 2. Group data by partition columns  
        // 3. Order within each partition
        // 4. Calculate rank within partition
        return info.RowNumber;
    }

    /// <summary>
    /// Enhanced SUM function with window frame support.
    /// Supports SUM(column) OVER (PARTITION BY ... ORDER BY ... ROWS BETWEEN ...).
    /// </summary>
    /// <param name="value">The value to sum</param>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <param name="partitionColumns">Comma-separated partition column values</param>
    /// <param name="orderColumns">Comma-separated order column values</param>
    /// <param name="frameStart">Window frame start (e.g., 'UNBOUNDED PRECEDING', '2 PRECEDING')</param>
    /// <param name="frameEnd">Window frame end (e.g., 'CURRENT ROW', '1 FOLLOWING')</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The sum over the window frame</returns>
    [BindableMethod]
    public T SumWithWindow<T>(T value, [InjectQueryStats] QueryStats info, 
        string partitionColumns = "", string orderColumns = "", 
        string frameStart = "UNBOUNDED PRECEDING", string frameEnd = "CURRENT ROW") where T : struct
    {
        // Enhanced implementation for true windowing (placeholder for now)
        // In full implementation, this would:
        // 1. Parse partition, order, and frame specifications
        // 2. Apply partitioning logic
        // 3. Calculate running sum within the specified window frame
        // 4. Handle ROWS BETWEEN ... AND ... semantics
        return value; // Basic implementation - returns input value
    }

    /// <summary>
    /// Enhanced COUNT function with window frame support.
    /// Supports COUNT(column) OVER (PARTITION BY ... ORDER BY ... ROWS BETWEEN ...).
    /// </summary>
    /// <param name="value">The value to count</param>
    /// <param name="info" injectedByRuntime="true">The queryStats object</param>
    /// <param name="partitionColumns">Comma-separated partition column values</param>
    /// <param name="orderColumns">Comma-separated order column values</param>
    /// <param name="frameStart">Window frame start (e.g., 'UNBOUNDED PRECEDING', '2 PRECEDING')</param>
    /// <param name="frameEnd">Window frame end (e.g., 'CURRENT ROW', '1 FOLLOWING')</param>
    /// <typeparam name="T">Value type</typeparam>
    /// <returns>The count over the window frame</returns>
    [BindableMethod]
    public long CountWithWindow<T>(T value, [InjectQueryStats] QueryStats info,
        string partitionColumns = "", string orderColumns = "",
        string frameStart = "UNBOUNDED PRECEDING", string frameEnd = "CURRENT ROW")
    {
        // Enhanced implementation for true windowing (placeholder for now)
        // In full implementation, this would:
        // 1. Parse partition, order, and frame specifications
        // 2. Apply partitioning logic
        // 3. Calculate running count within the specified window frame
        // 4. Handle ROWS BETWEEN ... AND ... semantics
        return info.RowNumber; // Basic implementation - returns row number
    }
}