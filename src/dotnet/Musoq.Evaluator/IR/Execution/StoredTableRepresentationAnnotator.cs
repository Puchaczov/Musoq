using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Applies planner-selected row shapes to every stored-row read.</summary>
internal sealed class StoredTableRepresentationAnnotator(
    IReadOnlyDictionary<int, ExecutionStoredTableRepresentationPlan> representations)
    : ExecutionIrRewriter
{
    public static ExecutionPlan Apply(
        ExecutionPlan plan,
        IReadOnlyList<ExecutionStoredTableRepresentationPlan> representations)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(representations);

        if (representations.Count == 0)
            return plan;

        var byTable = new Dictionary<int, ExecutionStoredTableRepresentationPlan>(representations.Count);
        foreach (var representation in representations)
            byTable.Add(representation.TableIndex, representation);

        return new StoredTableRepresentationAnnotator(byTable).RewritePlan(plan);
    }

    protected override ExecutionExpression RewriteStoredTableRows(ExecutionStoredTableRows expression)
    {
        if (!representations.TryGetValue(expression.TableIndex, out var representation) ||
            string.Equals(expression.GeneratedRowShape?.TypeName, representation.RowShape.TypeName, StringComparison.Ordinal))
            return expression;

        return expression with { GeneratedRowShape = representation.RowShape };
    }

    protected override ExecutionExpression RewriteCteCollectionInput(ExecutionCteCollectionInput expression)
    {
        var rewritten = (ExecutionCteCollectionInput)base.RewriteCteCollectionInput(expression);
        if (rewritten.Rows is not ExecutionStoredTableRows storedRows)
            return rewritten;

        if (!representations.TryGetValue(storedRows.TableIndex, out var representation))
            return rewritten;

        return rewritten with
        {
            Ownership = representation.Ownership,
            Lifetime = representation.Lifetime,
            MetricsStrategy = rewritten.LimitBinding == null
                ? ExecutionStructuralMetricsStrategy.None
                : ExecutionStructuralMetricsStrategy.TypedPrepass
        };
    }
}
