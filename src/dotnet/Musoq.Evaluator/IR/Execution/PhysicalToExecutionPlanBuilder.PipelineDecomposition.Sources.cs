using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static int CountSchemaScans(PhysicalNode node)
    {
        if (node is PhysicalSchemaScanNode)
            return 1;

        var count = 0;
        foreach (var child in node.Children)
            count += CountSchemaScans(child);

        return count;
    }

    private static ProjectedField[] CreateProjectedFields(OutputSchema outputSchema)
    {
        return outputSchema.Columns
            .Select(column => new ProjectedField(
                column.Name,
                new ColumnRef(string.Empty, column.Name, column.Type),
                column.Index))
            .ToArray();
    }

    private static SourcePipeline? DecomposeSourcePipeline(PhysicalNode input)
    {
        return PhysicalPipelineClassifier.TryDecomposeSourcePipeline(input, out var source)
            ? new SourcePipeline(source.Source, source.Filter)
            : null;
    }

    private static SourcePipeline? DecomposeWindowSourcePipeline(PhysicalNode input)
    {
        return DecomposeSourcePipeline(input) ??
               (IsAggregateSource(input) ? new SourcePipeline(input, null) : null);
    }
}
