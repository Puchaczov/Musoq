using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
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

    private static LoweringSourcePipeline? DecomposeSourcePipeline(PhysicalNode input)
    {
        return PhysicalPipelineClassifier.TryDecomposeSourcePipeline(input, out var source)
            ? new LoweringSourcePipeline(source.Source, source.Filter)
            : null;
    }

    private static LoweringSourcePipeline? DecomposeWindowSourcePipeline(PhysicalNode input)
    {
        return DecomposeSourcePipeline(input) ??
               (IsAggregateSource(input) ? new LoweringSourcePipeline(input, null) : null);
    }
}
