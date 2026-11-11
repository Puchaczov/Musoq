using System.Collections.Frozen;
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
    private static readonly ScalarReuseCostModel ReuseCost = new();

    public static SourceComputedProjectionAnalysis Analyze(
        PlanningContext context,
        string sourceId,
        SourceDescriptor descriptor,
        IReadOnlyList<RequiredColumnUsage> usages
    )
    {
        var required = SourceTransferCapabilities.QueryScopedRows;
        if (
            descriptor.ComputedProjectionCapabilities == SourceComputedProjectionCapabilities.None
            || (descriptor.TransferCapabilities & required) != required
            || (context.TargetSourceTransferCapabilities & required) != required
        )
            return SourceComputedProjectionAnalysis.Empty;

        var project = Unwrap(context.LogicalPlan) as ProjectNode;
        if (
            project is not { IsDistinct: false, Input: SchemaScanNode scan }
            || scan.SourceContextId != sourceId
            || scan.SourceConstructionType != null
        )
            return SourceComputedProjectionAnalysis.Empty;

        // Only the unconditional scan/project region is proven here. Other relational
        // and conditional regions deliberately keep their original evaluation site.
        var region = ScalarEvaluationRegion.Root(sourceId);
        if (
            !region.IsUnconditional
            || !region.AllowsHoisting
            || descriptor.Columns.Any(column => column.Stability != ColumnStability.Stable)
        )
            return SourceComputedProjectionAnalysis.Empty;

        var retainedInputs = usages
            .Where(static usage => usage.UsageReason != RequiredColumnUsageReason.Projection)
            .Select(static usage => usage.ColumnName)
            .ToFrozenSet(StringComparer.Ordinal);
        var inputsByExpression = new Dictionary<IrExpression, FrozenSet<string>>();
        var widthsByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var fields = new List<BoundProjection>();
        foreach (var occurrences in project.Fields.GroupBy(static field => field.Expression))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var expression = occurrences.Key;
            var scalar = SourceScalarProjectionConverter.Convert(expression, scan, descriptor);
            if (
                scalar is null
                || !SourceScalarTotality.IsTotal(scalar)
                || !ExpressionStabilityAnalyzer.IsStable(expression)
            )
                // A mixed projection may require the declared entity or change observable
                // evaluation order. Prove the entire region before offering any field.
                return SourceComputedProjectionAnalysis.Empty;
            var inputs = CollectInputs(context, expression, widthsByName);
            inputsByExpression.Add(expression, inputs);
            if (expression is not ColumnRef && inputs.Count != 0)
                fields.Add(
                    new BoundProjection(expression, scalar, occurrences.First().OutputIndex, occurrences.Count())
                );
        }

        var bound = new BoundProjectionRegion(region, fields, inputsByExpression, retainedInputs, widthsByName);
        return SelectOffers(context, descriptor, bound);
    }

    private static SourceComputedProjectionAnalysis SelectOffers(
        PlanningContext context,
        SourceDescriptor descriptor,
        BoundProjectionRegion bound
    )
    {
        if (bound.Fields.Count == 0)
            return SourceComputedProjectionAnalysis.Empty;
        var inputUses = bound
            .InputsByExpression.Values.SelectMany(static inputs => inputs)
            .GroupBy(static input => input, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);
        var result = new List<SourceComputedProjectionBinding>();
        var names = descriptor
            .Columns.Select(static column => column.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingWidth = bound.WidthsByName.Values.Sum();
        var declined = 0;
        var reservedGrowth = 0;
        foreach (var field in bound.Fields)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var name = "__computed" + field.OutputIndex.ToString(CultureInfo.InvariantCulture);
            while (!names.Add(name))
                name += "_";
            var projection = new SourceComputedProjection(name, field.Scalar, field.Expression.ReturnType);
            if (
                !SourceComputedProjectionFacts.CanProviderEvaluate(
                    projection,
                    descriptor.ComputedProjectionCapabilities
                )
            )
                continue;
            var inputs = bound.InputsByExpression[field.Expression];
            var releasedWidth = inputs
                .Where(input => inputUses[input] == 1 && !bound.RetainedInputs.Contains(input))
                .Sum(input => bound.WidthsByName[input]);
            var growth = TransferBytes(projection.ResultType) - releasedWidth;
            if (
                growth > 0
                && !AllowsGrowth(field, projection, inputs, bound.Region, (existingWidth, reservedGrowth + growth))
            )
            {
                declined++;
                continue;
            }
            reservedGrowth += Math.Max(0, growth);
            result.Add(new(field.Expression, projection));
        }

        return result.Count == 0
            ? SourceComputedProjectionAnalysis.Empty with
            {
                DeclinedForTransferCost = declined,
            }
            : new SourceComputedProjectionAnalysis(
                result.AsReadOnly(),
                bound.InputsByExpression.ToFrozenDictionary(),
                bound.RetainedInputs,
                declined
            );
    }

    private static bool AllowsGrowth(
        BoundProjection field,
        SourceComputedProjection projection,
        FrozenSet<string> inputs,
        ScalarEvaluationRegion region,
        (int Existing, int Growth) transfer
    )
    {
        if (field.Uses < ReuseCost.MinimumDynamicUses)
            return false;
        var candidate = new ScalarReuseCandidate(
            SourceScalarExpressionFingerprint.Compute(projection.Expression),
            projection.ResultType,
            ColumnStability.Stable,
            inputs.Order(StringComparer.Ordinal).ToArray(),
            region.OwnerScope,
            region,
            field.Uses,
            field.Uses,
            transfer.Growth
        );
        return ReuseCost.ShouldMaterialize(candidate, transfer.Existing, field.Uses);
    }

    private static FrozenSet<string> CollectInputs(
        PlanningContext context,
        IrExpression expression,
        Dictionary<string, int> widthsByName
    )
    {
        var inputs = new HashSet<string>(StringComparer.Ordinal);
        foreach (
            var column in IrExpressionTraversal
                .SelfAndDescendants(expression, context.CancellationToken)
                .OfType<ColumnRef>()
        )
        {
            inputs.Add(column.ColumnName);
            widthsByName.TryAdd(column.ColumnName, TransferBytes(column.ReturnType));
        }
        return inputs.ToFrozenSet(StringComparer.Ordinal);
    }

    // A field read and store move at least one machine word in the transfer cost model.
    private static int TransferBytes(Type type) => Math.Max(IntPtr.Size, SourceRowPayload.EstimateFieldBytes(type));

    private sealed record BoundProjection(
        IrExpression Expression,
        SourceScalarExpression Scalar,
        int OutputIndex,
        int Uses
    );

    private sealed record BoundProjectionRegion(
        ScalarEvaluationRegion Region,
        IReadOnlyList<BoundProjection> Fields,
        IReadOnlyDictionary<IrExpression, FrozenSet<string>> InputsByExpression,
        FrozenSet<string> RetainedInputs,
        IReadOnlyDictionary<string, int> WidthsByName
    );

    private static LogicalNode Unwrap(LogicalNode node) =>
        node is MultiStatementNode { Statements.Length: 1 } root ? root.Statements[0] : node;
}
