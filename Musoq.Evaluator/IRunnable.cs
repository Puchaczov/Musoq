using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator;

/// <summary>
/// Represents a compiled and executable query that can be run against data sources.
/// Implementations of this interface are generated at runtime from parsed SQL queries.
/// </summary>
/// <remarks>
/// The IRunnable interface provides the contract for executing compiled queries with:
/// - Schema provider integration for accessing various data sources
/// - Query metadata and environment variable management
/// - Event-based tracking of query execution phases and data source progress
/// - Both synchronous and asynchronous execution capabilities
/// </remarks>
public interface IRunnable
{
    /// <summary>
    /// Gets or sets the schema provider used to access data sources during query execution.
    /// </summary>
    /// <remarks>
    /// The schema provider is responsible for resolving schema methods and table sources
    /// referenced in the query. Must be set before calling Run or RunAsync.
    /// </remarks>
    ISchemaProvider Provider { get; set; }
        
    /// <summary>
    /// Gets or sets a dictionary of positional environment variables indexed by position.
    /// Each entry maps string key-value pairs representing environment-level configuration.
    /// </summary>
    /// <remarks>
    /// Positional environment variables allow different sets of environment context
    /// for different FROM clauses in a single query or across complex query structures.
    /// The key is the position (usually 0, 1, 2, etc.) and the value is a dictionary of
    /// string variables that can be referenced within the query.
    /// </remarks>
    IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }
        
    /// <summary>
    /// Gets or sets metadata about all queries (main query and subqueries/CTEs) involved in execution.
    /// Maps query identifiers to tuples containing the source node, columns, WHERE clause, and type information.
    /// </summary>
    /// <remarks>
    /// This provides the query execution engine with information about:
    /// - The FROM node for each query/subquery/CTE
    /// - Which columns are used by each query
    /// - The WHERE clause conditions (or AllTrue if no WHERE)
    /// - Whether types were explicitly provided or inferred
    /// 
    /// Used for query optimization, column pruning, and type inference.
    /// </remarks>
    IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation { get; set; }
    
    /// <summary>
    /// Gets or sets the logger used for diagnostic and error logging during query execution.
    /// </summary>
    /// <remarks>
    /// The logger is used throughout query execution to record:
    /// - Execution phases and timing information
    /// - Schema resolution and type inference details
    /// - Warnings and errors during execution
    /// 
    /// Must be set before calling Run or RunAsync. If not set, logging will be suppressed.
    /// </remarks>
    ILogger Logger { get; set; }
    
    /// <summary>
    /// Event raised when the query execution phase changes (Begin, From, Where, GroupBy, Select, End).
    /// Fires once per phase to allow monitoring of query execution progress.
    /// </summary>
    /// <remarks>
    /// Attach event handlers before calling Run or RunAsync to receive notifications
    /// about which phase of execution the query is currently in.
    /// 
    /// Event args contain:
    /// - QueryId: Unique identifier of the query (CTE name or positional index)
    /// - Phase: The current execution phase
    /// 
    /// Phases fire in this order: Begin → From → [Where] → [GroupBy] → Select → End
    /// (Where and GroupBy are conditional based on query structure)
    /// </remarks>
    event QueryPhaseEventHandler PhaseChanged;
    
    /// <summary>
    /// Event raised when data source progress changes during row processing.
    /// Fires for Begin, RowsKnown, RowsRead (per row), and End phases.
    /// </summary>
    /// <remarks>
    /// Attach event handlers before calling Run or RunAsync to receive notifications
    /// about data source activity during query execution.
    /// 
    /// Event args contain:
    /// - QueryId: The query that triggered this data source access
    /// - DataSourceName: The name of the data source being accessed
    /// - Phase: The phase (Begin, RowsKnown, RowsRead, or End)
    /// - TotalRows: Total rows available/processed (available for RowsKnown, RowsRead, End)
    /// - RowsProcessed: Number of rows processed so far (available for RowsRead, End)
    /// 
    /// Typical sequence:
    /// 1. Begin - Data source access initiated
    /// 2. RowsKnown - Total row count determined
    /// 3. RowsRead (multiple times) - Fired after each row is processed
    /// 4. End - Data source access complete
    /// </remarks>
    event DataSourceEventHandler DataSourceProgress;

    /// <summary>
    /// Executes the compiled query synchronously and returns the results as a table.
    /// </summary>
    /// <param name="token">Cancellation token to allow interrupting the query execution.</param>
    /// <returns>A Table containing the query results.</returns>
    /// <remarks>
    /// Before calling Run:
    /// - Set Provider property to provide access to data sources
    /// - Set Logger property (optional, but recommended)
    /// - Set PositionalEnvironmentVariables if the query references environment variables
    /// - Attach PhaseChanged and/or DataSourceProgress event handlers if you want execution tracking
    /// 
    /// The query executes synchronously, blocking until results are available or cancelled.
    /// Use RunAsync for non-blocking execution in async contexts.
    /// </remarks>
    Table Run(CancellationToken token);
    
    /// <summary>
    /// Executes the compiled query asynchronously and returns the results as a table.
    /// </summary>
    /// <param name="token">Cancellation token to allow interrupting the query execution.</param>
    /// <returns>A task that completes when the query finishes, containing the result table.</returns>
    /// <remarks>
    /// This method wraps the synchronous Run method in a Task.Run call, allowing
    /// query execution to happen on a background thread without blocking the calling context.
    /// 
    /// It naturally inherits all tracking from Run, so PhaseChanged and DataSourceProgress
    /// events will still fire appropriately during async execution.
    /// 
    /// See Run() documentation for preparation steps and remarks.
    /// </remarks>
    Task<Table> RunAsync(CancellationToken token) => Task.Run(() => Run(token), token);
}