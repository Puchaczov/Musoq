using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceInteractionPlan(
    string SourceContextId,
    string Alias,
    SourceShapeKind ShapeKind,
    SourceColumnContract ColumnContract,
    SourcePredicateContract PredicateContract,
    SourceArgumentMode ArgumentMode,
    ISchemaColumn[] QuerySourceColumns,
    WhereNode? QuerySourceWhereNode,
    SourcePlanRequest SourcePlanRequest,
    PlanningConfidence Confidence,
    string ShapeReason,
    string ColumnReason,
    string PredicateReason,
    string SourcePlanRequestReason,
    string ArgumentReason);
