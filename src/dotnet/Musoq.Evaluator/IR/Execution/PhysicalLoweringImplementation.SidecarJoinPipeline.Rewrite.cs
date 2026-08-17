using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ProjectedField[]? RewriteSidecarJoinProjectedFields(
        IReadOnlyList<ProjectedField> fields,
        IReadOnlyDictionary<string, IrExpression>? projectedExpressions,
        PhysicalCteRefNode? cteRef)
    {
        if (cteRef == null)
            return fields.ToArray();

        if (projectedExpressions == null)
            return null;

        var rewritten = new ProjectedField[fields.Count];
        for (var index = 0; index < fields.Count; index++)
        {
            var expression = RewriteFinalJoinExpression(fields[index].Expression, projectedExpressions, cteRef);
            if (expression == null)
                return null;

            rewritten[index] = fields[index] with { Expression = expression };
        }

        return rewritten;
    }

    private static PhysicalFilterNode? RewriteSidecarJoinFilter(
        PhysicalFilterNode? filter,
        IReadOnlyDictionary<string, IrExpression>? projectedExpressions,
        PhysicalCteRefNode? cteRef)
    {
        if (filter == null)
            return null;

        if (cteRef == null)
            return filter;

        if (projectedExpressions == null)
            return null;

        var predicate = RewriteFinalJoinExpression(filter.Predicate, projectedExpressions, cteRef);
        return predicate == null ? null : filter with { Predicate = predicate };
    }

    private static IrExpression? RewriteSidecarJoinExpression(
        IrExpression? expression,
        IReadOnlyDictionary<string, IrExpression>? projectedExpressions,
        PhysicalCteRefNode? cteRef)
    {
        if (expression == null)
            return null;

        if (cteRef == null)
            return expression;

        return projectedExpressions == null
            ? null
            : RewriteFinalJoinExpression(expression, projectedExpressions, cteRef);
    }

    private static IrExpression[]? RewriteSidecarJoinExpressions(
        IReadOnlyList<IrExpression> expressions,
        IReadOnlyDictionary<string, IrExpression>? projectedExpressions,
        PhysicalCteRefNode? cteRef)
    {
        if (cteRef == null)
            return expressions.ToArray();

        if (projectedExpressions == null)
            return null;

        var rewritten = new IrExpression[expressions.Count];
        for (var index = 0; index < expressions.Count; index++)
        {
            var expression = RewriteFinalJoinExpression(expressions[index], projectedExpressions, cteRef);
            if (expression == null)
                return null;

            rewritten[index] = expression;
        }

        return rewritten;
    }

}
