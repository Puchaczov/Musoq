using System.Collections.Generic;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourcePredicatePlanner
{
    private sealed record SourcePredicateTransferContext(
        string Alias,
        IReadOnlyDictionary<string, ISchemaColumn[]> Columns,
        bool HasDirectScanFilter);

    private static HashSet<string> FindDirectFilterSources(LogicalNode? node)
    {
        var sources = new HashSet<string>(StringComparer.Ordinal);
        if (node != null)
            Visit(node);
        return sources;

        void Visit(LogicalNode current)
        {
            if (current is FilterNode { Input: SchemaScanNode { SourceContextId: { } id } })
                sources.Add(id);
            foreach (var child in current.Children)
                Visit(child);
        }
    }

    // Regex matching can time out: it is not generally safe to move across joins or
    // conditional evaluation boundaries. Only direct scan filtering is offered to an
    // opt-in provider. The ordinary movement planner retains its totality requirement.
    private static bool IsSourceTransferSafe(IrExpression predicate, SourcePredicateTransferContext context)
    {
        if (!context.HasDirectScanFilter)
            return PredicateMovementPlanner.IsTransferSafe(predicate);
        if (!SourcePredicateGraphValidator.IsWithinBudget(predicate, System.Threading.CancellationToken.None) ||
            !ExpressionStabilityAnalyzer.IsStable(predicate))
            return false;
        return IsSafe(predicate);

        static bool IsSafe(IrExpression expression) => expression switch
        {
            PatternMatch { Kind: PatternKind.RLike, Expression: ColumnRef, Pattern: Literal { Value: string } } => true,
            BinaryOp { Kind: BinaryOpKind.And or BinaryOpKind.Or } binary => IsSafe(binary.Left) && IsSafe(binary.Right),
            UnaryOp { Kind: UnaryOpKind.Not } unary => IsSafe(unary.Operand),
            _ => PredicateMovementPlanner.IsTransferSafe(expression)
        };
    }
}
