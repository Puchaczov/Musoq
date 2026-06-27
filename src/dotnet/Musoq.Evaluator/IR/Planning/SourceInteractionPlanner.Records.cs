using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceInteractionPlanner
{
    private sealed record SourceInteractionShape(SourceShapeKind Kind, PlanningConfidence Confidence, string Reason);

    private sealed record SourceInteractionColumns(
        SourceColumnContract Contract,
        ISchemaColumn[] Columns,
        PlanningConfidence Confidence,
        string Reason);

    private sealed record SourceInteractionPredicate(
        SourcePredicateContract Contract,
        WhereNode? WhereNode,
        PlanningConfidence Confidence,
        string Reason);

    private sealed record SourceInteractionArguments(
        SourceArgumentMode Mode,
        PlanningConfidence Confidence,
        string Reason);

    private sealed record ArgumentReferenceResult(IReadOnlySet<string> Aliases, bool HasUnknownExpression);
}
