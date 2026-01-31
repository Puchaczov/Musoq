using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Schema.Api;

namespace Musoq.Schema;

/// <summary>
/// Provides runtime context for data source row sources during query execution.
/// Contains query metadata, filtering information, and optimization hints that
/// data sources can use to optimize data fetching (e.g., predicate pushdown, pagination).
/// </summary>
public class RuntimeContext(
    string queryId,
    CancellationToken endWorkToken,
    IReadOnlyCollection<ISchemaColumn> originallyInferredColumns,
    IReadOnlyDictionary<string, string> environmentVariables,
    QuerySourceInfo querySourceInfo,
    ILogger logger,
    DataSourceEventHandler dataSourceProgressCallback = null)
{
    /// <summary>
    /// Gets the unique identifier for this query execution.
    /// </summary>
    public string QueryId { get; } = queryId;

    /// <summary>
    /// Gets the cancellation token that signals when the query should stop executing.
    /// Data sources should check this token regularly and stop fetching data when cancelled.
    /// </summary>
    public CancellationToken EndWorkToken { get; } = endWorkToken;

    /// <summary>
    /// Gets all columns that were inferred/available for this data source.
    /// This represents the full schema available from the data source.
    /// </summary>
    public IReadOnlyCollection<ISchemaColumn> AllColumns { get; } = originallyInferredColumns;

    /// <summary>
    /// Gets environment variables specific to this query execution.
    /// Can contain API keys, connection strings, or other configuration.
    /// </summary>
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; } = environmentVariables;

    /// <summary>
    /// Gets detailed query source information including the FROM node, used columns, WHERE clause,
    /// type information, and optimization hints. Data sources can use this for:
    /// - Predicate pushdown: Use WhereNode to filter data at the source (e.g., API query parameters)
    /// - Column projection: Use Columns to only fetch required fields
    /// - Pagination: Use QueryHints for SKIP/TAKE optimization
    /// </summary>
    public QuerySourceInfo QuerySourceInfo { get; } = querySourceInfo ?? QuerySourceInfo.Empty;

    /// <summary>
    /// Gets the logger for diagnostic output.
    /// </summary>
    public ILogger Logger { get; } = logger;

    /// <summary>
    /// Gets query-level optimization hints including SKIP/TAKE values.
    /// This is a convenience property that delegates to QuerySourceInfo.QueryHints.
    /// </summary>
    /// <remarks>
    /// - SkipValue: Number of rows to skip (for offset-based pagination)
    /// - TakeValue: Maximum number of rows needed (allows early termination)
    /// - IsDistinct: Whether the query requires distinct results
    /// 
    /// Note: These are hints, not guarantees. The engine will still apply
    /// SKIP/TAKE after receiving data, so data sources can ignore these
    /// if they can't implement them efficiently.
    /// </remarks>
    public QueryHints QueryHints => QuerySourceInfo.QueryHints;

    public void ReportDataSourceBegin(string dataSourceName)
    {
        dataSourceProgressCallback?.Invoke(this,
            new DataSourceEventArgs(QueryId, dataSourceName, DataSourcePhase.Begin));
    }

    public void ReportDataSourceRowsKnown(string dataSourceName, long totalRows)
    {
        dataSourceProgressCallback?.Invoke(this,
            new DataSourceEventArgs(QueryId, dataSourceName, DataSourcePhase.RowsKnown, totalRows));
    }

    public void ReportDataSourceRowsRead(string dataSourceName, long rowsProcessed, long? totalRows = null)
    {
        dataSourceProgressCallback?.Invoke(this,
            new DataSourceEventArgs(QueryId, dataSourceName, DataSourcePhase.RowsRead, totalRows, rowsProcessed));
    }

    public void ReportDataSourceEnd(string dataSourceName, long? totalRowsProcessed = null)
    {
        dataSourceProgressCallback?.Invoke(this,
            new DataSourceEventArgs(QueryId, dataSourceName, DataSourcePhase.End, totalRowsProcessed,
                totalRowsProcessed));
    }
}
