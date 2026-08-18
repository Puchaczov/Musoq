using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourcePlanProperties(
    string SourceContextId,
    string Alias,
    string SchemaName,
    string MethodName,
    string[] RequiredColumns,
    IrExpression[] PushedPredicates,
    string[] ProjectedColumns,
    ISchemaColumn[] ProjectedSchemaColumns,
    SourceQueryRowProjection QueryRowProjection,
    PlanningConfidence ShapeConfidence,
    string ShapeReason);

internal enum SourceProjectionState
{
    Unavailable,
    Exact
}

internal sealed record SourceQueryRowProjection(
    SourceProjectionState State,
    IReadOnlyList<ISchemaColumn> Columns,
    string Reason)
{
    public static SourceQueryRowProjection Exact(IReadOnlyList<ISchemaColumn> columns, string reason)
    {
        ArgumentNullException.ThrowIfNull(columns);
        return new SourceQueryRowProjection(
            SourceProjectionState.Exact,
            Array.AsReadOnly(columns.ToArray()),
            reason);
    }

    public static SourceQueryRowProjection Unavailable(string reason)
    {
        return new SourceQueryRowProjection(SourceProjectionState.Unavailable, [], reason);
    }
}
