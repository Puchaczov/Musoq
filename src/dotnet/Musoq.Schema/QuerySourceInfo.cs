using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema.Api;

namespace Musoq.Schema;

/// <summary>
///     Contains detailed information about a query's data source, including the FROM node,
///     projected columns, WHERE clause for predicate pushdown, and optimization hints.
/// </summary>
public class QuerySourceInfo
{
    /// <summary>
    ///     Creates an empty QuerySourceInfo with no optimization hints.
    /// </summary>
    public static readonly QuerySourceInfo Empty = new(null, [], null, false, QueryHints.Empty);

    /// <summary>
    ///     Creates a new QuerySourceInfo with the specified values.
    /// </summary>
    public QuerySourceInfo(
        SchemaFromNode? fromNode,
        IReadOnlyCollection<ISchemaColumn> columns,
        WhereNode? whereNode,
        bool hasExternallyProvidedTypes,
        QueryHints queryHints)
    {
        FromNode = fromNode;
        Columns = columns ?? [];
        WhereNode = whereNode;
        HasExternallyProvidedTypes = hasExternallyProvidedTypes;
        QueryHints = queryHints;
    }

    /// <summary>
    ///     Gets the FROM node containing schema, method, and parameters for this data source.
    /// </summary>
    public SchemaFromNode? FromNode { get; }

    /// <summary>
    ///     Gets the columns that are actually used by the query.
    ///     Data sources can use this for column projection optimization - only fetching required fields.
    /// </summary>
    public IReadOnlyCollection<ISchemaColumn> Columns { get; }

    /// <summary>
    ///     Gets the WHERE clause node containing predicates safe to push to this data source.
    ///     Predicates involving other data sources or complex expressions are filtered out.
    ///     If no predicates apply, this will be a simple "1 = 1" expression.
    /// </summary>
    public WhereNode? WhereNode { get; }

    /// <summary>
    ///     Gets whether the data source types were provided externally (e.g., via explicit schema).
    /// </summary>
    public bool HasExternallyProvidedTypes { get; }

    /// <summary>
    ///     Gets query-level optimization hints including SKIP/TAKE values and DISTINCT flag.
    ///     Data sources can use these to implement server-side pagination
    ///     and limit the amount of data fetched from external APIs.
    /// </summary>
    /// <remarks>
    ///     - SkipValue: Number of rows to skip (for offset-based pagination)
    ///     - TakeValue: Maximum number of rows needed (allows early termination)
    ///     - IsDistinct: Whether the query requires distinct results
    ///     Note: These are hints, not guarantees. The engine will still apply
    ///     SKIP/TAKE after receiving data, so data sources can ignore these
    ///     if they can't implement them efficiently.
    /// </remarks>
    public QueryHints QueryHints { get; }

    /// <summary>
    ///     Creates a new QuerySourceInfo from legacy tuple format (for backward compatibility).
    /// </summary>
    public static QuerySourceInfo FromTuple(
        (SchemaFromNode? FromNode, IReadOnlyCollection<ISchemaColumn> Columns, WhereNode? WhereNode, bool
            HasExternallyProvidedTypes) tuple,
        QueryHints? queryHints = null)
    {
        return new QuerySourceInfo(
            tuple.FromNode,
            tuple.Columns,
            tuple.WhereNode,
            tuple.HasExternallyProvidedTypes,
            queryHints ?? QueryHints.Empty);
    }
}
