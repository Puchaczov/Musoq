using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private OutputSchema ResolveScanOutput(SchemaScanNode scan)
    {
        var transfer = ResolveSourceTransferStrategy(scan.SourceContextId);
        if (transfer is null || transfer.ComputedProjections.Count == 0)
            return scan.OutputSchema;
        return new OutputSchema([.. scan.OutputSchema.Columns, .. transfer.ComputedProjections.Select((binding, index) =>
            new ColumnSchema(binding.Projection.Name, binding.Projection.ResultType, scan.OutputSchema.Columns.Length + index))]);
    }

    private ProjectedField[] ResolveProjectedFields(ProjectNode project)
    {
        if (project.Input is not SchemaScanNode scan ||
            ResolveSourceTransferStrategy(scan.SourceContextId) is not { ComputedProjections.Count: > 0 } transfer)
            return project.Fields;
        return project.Fields.Select(field =>
        {
            var binding = transfer.ComputedProjections.FirstOrDefault(item => item.Expression == field.Expression);
            return binding is null ? field : field with
            {
                Expression = new ColumnRef(scan.Alias, binding.Projection.Name, binding.Projection.ResultType)
            };
        }).ToArray();
    }
}
