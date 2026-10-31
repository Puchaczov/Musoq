using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.TypedOutput;

namespace Musoq.Converter;

internal static class TypedOutputSchema
{
    public static TypedOutputColumn[] Resolve(LogicalNode? plan, ExecutionPlan? executionPlan)
    {
        if (executionPlan?.FinalResult is { } finalResult)
            return finalResult.ColumnMetadata.Fields.Select(static column =>
                new TypedOutputColumn(column.Name, column.Index, column.Type.ResolveClrType())).ToArray();
        if (plan is null)
            return [];

        while (plan.OutputSchema.Columns.Length == 0 && plan.Children.Count != 0)
            plan = plan.Children[^1];

        return plan.OutputSchema.Columns.Select(static column =>
            new TypedOutputColumn(column.Name, column.Index, column.Type)).ToArray();
    }
}
