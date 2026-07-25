using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionPlanBuildResult CreateUnsupported(PhysicalNode node)
    {
        if (ContainsNode<PhysicalWindowNode>(node))
        {
            return ExecutionPlanBuildResult.CreateUnsupported(
                $"Execution IR window lowering currently supports RowNumber, Rank, DenseRank, Ntile, Lag, Lead, resolved plugin window factories, and the built-in Sum, Count, Avg, Min, Max, FirstValue, LastValue, and NthValue helpers over materialized schema, CTE, dynamic, and decomposed join sources with optional PARTITION BY and QUALIFY. Found {FormatPhysicalShape(node)}.");
        }

        if (ContainsNode<PhysicalAggregateOnlyNode>(node) ||
            ContainsNode<PhysicalSingleKeyAggregateNode>(node) ||
            ContainsNode<PhysicalValueTupleAggregateNode>(node))
        {
            return ExecutionPlanBuildResult.CreateUnsupported(
                $"Execution IR aggregate lowering currently supports direct aggregate-only projections and direct grouped aggregate projections with optional HAVING and projected aggregate ORDER BY keys. Found {FormatPhysicalShape(node)}.");
        }

        if (ContainsNode<PhysicalSetOperationNode>(node))
        {
            return ExecutionPlanBuildResult.CreateUnsupported(
                "Execution IR set-operation lowering supports arms that lower through supported table-producing Execution IR paths with optional Sort/Skip/Take wrappers; one or more arms still contained unsupported physical shapes.");
        }

        return ExecutionPlanBuildResult.CreateUnsupported(
            $"Execution IR lowering currently supports Project -> Filter? -> (SchemaScan|CteRef|flat inner Join), aggregate-only direct projections, simple set-operation arms, optional Sort/Skip/Take wrappers, and simple CTE definitions. Found {node.GetType().Name}.");
    }

    private static bool ContainsNode<TNode>(PhysicalNode node)
        where TNode : PhysicalNode
    {
        if (node is TNode)
            return true;

        foreach (var child in node.Children)
        {
            if (ContainsNode<TNode>(child))
                return true;
        }

        return false;
    }

    private static string FormatPhysicalShape(PhysicalNode node)
    {
        if (node.Children.Count == 0)
            return node.GetType().Name;

        return $"{node.GetType().Name}({string.Join(", ", node.Children.Select(FormatPhysicalShape))})";
    }
}
