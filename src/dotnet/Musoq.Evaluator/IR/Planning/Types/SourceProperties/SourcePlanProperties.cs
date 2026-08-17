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
    PlanningConfidence ShapeConfidence,
    string ShapeReason);
