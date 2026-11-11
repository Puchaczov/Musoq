using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal sealed record SourceComputedProjectionBinding(IrExpression Expression, SourceComputedProjection Projection);

internal static class SourceComputedProjectionPlanner
{
    public static IReadOnlyList<SourceComputedProjectionBinding> Candidates(
        PlanningContext context, string sourceId, SourceDescriptor descriptor)
    {
        var required = SourceTransferCapabilities.QueryScopedRows;
        if (descriptor.ComputedProjectionCapabilities == SourceComputedProjectionCapabilities.None ||
            (descriptor.TransferCapabilities & required) != required ||
            (context.TargetSourceTransferCapabilities & required) != required)
            return [];

        var project = Unwrap(context.LogicalPlan) as ProjectNode;
        if (project is not { IsDistinct: false, Input: SchemaScanNode scan } ||
            scan.SourceContextId != sourceId || scan.SourceConstructionType != null)
            return [];

        // Only the unconditional scan/project region is proven here. Other relational
        // and conditional regions deliberately keep their original evaluation site.
        var region = ScalarEvaluationRegion.Root(sourceId);
        if (!region.IsUnconditional || !region.AllowsHoisting ||
            descriptor.Columns.Any(column => column.Stability != ColumnStability.Stable))
            return [];

        var result = new List<SourceComputedProjectionBinding>();
        var names = descriptor.Columns.Select(column => column.ColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var field in project.Fields)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var expression = SourceScalarProjectionConverter.Convert(field.Expression, scan, descriptor);
            if (expression is null || !SourceScalarTotality.IsTotal(expression) || !ExpressionStabilityAnalyzer.IsStable(field.Expression))
                // A mixed projection may require the declared entity or change observable
                // evaluation order. Prove the entire region before offering any field.
                return [];
            if (field.Expression is ColumnRef)
                continue;
            var name = "__computed" + field.OutputIndex.ToString(CultureInfo.InvariantCulture);
            while (!names.Add(name))
                name += "_";
            var projection = new SourceComputedProjection(name, expression, field.Expression.ReturnType);
            if (SourceComputedProjectionFacts.CanProviderEvaluate(projection, descriptor.ComputedProjectionCapabilities))
                result.Add(new(field.Expression, projection));
        }
        return result.AsReadOnly();
    }

    private static LogicalNode Unwrap(LogicalNode node) => node is MultiStatementNode { Statements.Length: 1 } root
        ? root.Statements[0] : node;
}
