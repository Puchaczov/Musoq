using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static class ExecutionStrategyPipelineDecomposer
{
    public static SupportedPipeline? TryDecomposeSupportedPipeline(PhysicalNode node)
    {
        var postOperations = new List<PhysicalNode>();
        var current = node;

        while (PhysicalPipelineClassifier.GetPostOperationInput(current) is { } input)
        {
            postOperations.Add(current);
            current = input;
        }

        if (current is not PhysicalProjectNode project)
            return null;

        var source = DecomposeSourcePipeline(project.Input);
        return source == null
            ? null
            : new SupportedPipeline(project, source.Source, source.Filter, postOperations);
    }

    public static SingleKeyAggregatePipeline? TryDecomposeSingleKeyAggregatePipeline(PhysicalNode node)
    {
        var postOperations = new List<PhysicalNode>();
        var current = node;

        while (PhysicalPipelineClassifier.GetPostOperationInput(current) is { } input)
        {
            postOperations.Add(current);
            current = input;
        }

        if (current is not PhysicalProjectNode project)
            return null;

        return project.Input switch
        {
            PhysicalSingleKeyAggregateNode aggregate when DecomposeSourcePipeline(aggregate.Input) is { } source =>
                new SingleKeyAggregatePipeline(project, aggregate, source.Source, source.Filter, postOperations),
            PhysicalHavingFilterNode { Input: PhysicalSingleKeyAggregateNode aggregate } when DecomposeSourcePipeline(aggregate.Input) is { } source =>
                new SingleKeyAggregatePipeline(project, aggregate, source.Source, source.Filter, postOperations),
            _ => null
        };
    }

    public static SourcePipeline? DecomposeSourcePipeline(PhysicalNode input)
    {
        return PhysicalPipelineClassifier.TryDecomposeSourcePipeline(input, out var source)
            ? new SourcePipeline(source.Source, source.Filter)
            : null;
    }

    public static PhysicalNode UnwrapSingleStatement(PhysicalNode node)
    {
        while (node is PhysicalMultiStatementNode { Statements.Length: 1 } multiStatement)
            node = multiStatement.Statements[0];

        return node;
    }

    public static TableRowShape CreateTableRowShape(PhysicalCteRefNode cteRef)
    {
        return new TableRowShape(
            cteRef.Alias,
            cteRef.OutputSchema.Columns.Select(column => new FieldBinding(
                column.Name,
                $"{cteRef.Alias}.{column.Name}",
                column.Index,
                column.Type,
                FieldNullability.Unknown,
                new PositionalAccess(column.Index))).ToArray());
    }

}
