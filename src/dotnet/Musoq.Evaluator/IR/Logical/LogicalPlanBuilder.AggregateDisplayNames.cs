using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private static string? GetAggregateDisplayName(AccessMethodNode node)
    {
        return node.Arguments.Args.Length > 0 &&
               node.Arguments.Args[0] is AggregateIdentifierNode aggregateIdentifier
            ? aggregateIdentifier.DisplayName
            : null;
    }
}
