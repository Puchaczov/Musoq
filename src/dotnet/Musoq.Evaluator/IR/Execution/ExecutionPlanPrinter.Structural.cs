using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static void AppendStoredTableRepresentations(
        StringBuilder builder,
        IReadOnlyList<ExecutionStoredTableRepresentationPlan> representations,
        ExecutionBlock block)
    {
        var structuralTableIndexes = ExecutionIrAnalysis
            .CollectExpressions<ExecutionCteCollectionInput>(block)
            .Select(static input => input.Rows)
            .OfType<ExecutionStoredTableRows>()
            .Select(static rows => rows.TableIndex)
            .ToHashSet();
        var selected = representations
            .Where(item => structuralTableIndexes.Count > 0
                ? structuralTableIndexes.Contains(item.TableIndex)
                : item.Ownership != ExecutionStructuralOwnershipMode.ConstructFresh ||
                  item.Lifetime != ExecutionStructuralPreparationLifetime.Execution)
            .OrderBy(static item => item.TableIndex)
            .ToArray();
        if (selected.Length == 0)
            return;

        builder.AppendLine("  StoredTableRepresentations");
        foreach (var representation in selected)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"    [{representation.TableIndex}] {representation.Kind}; row={representation.RowShape.TypeName}; " +
                $"ownership={representation.Ownership}; lifetime={representation.Lifetime}");
        }
    }

    private static void AppendStructuralPreparationPlans(
        StringBuilder builder,
        ExecutionBlock block)
    {
        var plans = ExecutionIrAnalysis
            .CollectNodes<ExecutionPrepareStructuralInput>(block)
            .Select(static node => node.Plan)
            .Concat(ExecutionIrAnalysis
                .CollectExpressions<ExecutionCteCollectionInput>(block)
                .Where(static input => input.ConstructionPlan != null)
                .Select(static input => input.ConstructionPlan!))
            .Distinct()
            .ToArray();
        if (plans.Length == 0)
            return;

        builder.AppendLine("  StructuralPreparation");
        foreach (var plan in plans)
        {
            builder.AppendLine(
                $"    {plan.TargetType.DisplayName}; constructor={plan.Constructor.StableId}; " +
                $"shape={plan.InputShape.CanonicalType}; " +
                $"origin={plan.Origin}; lifetime={plan.Lifetime}; metrics={plan.MetricsStrategy}; " +
                $"ownership={plan.Ownership}; limits={plan.Limits}; " +
                $"defaults={string.Join(',', plan.Defaults.Select(static value => value?.ToString() ?? "-"))}");
        }
    }
}
