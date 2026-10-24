using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization.Execution;

namespace Musoq.Evaluator.IR.Execution;

internal static class SubqueryExecutionLowering
{
    internal static ExecutionSubquery Convert(SubqueryExpression expression,
        IReadOnlyDictionary<string, RowShape> sourceShapes, IReadOnlyDictionary<string, int>? cteIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        return Create(expression,
            expression.Bindings.Select(binding => ExecutionExpressionConverter.Convert(binding, sourceShapes, cteIndexes, methodTargets)).ToArray(),
            expression.Operand is null ? null : ExecutionExpressionConverter.Convert(expression.Operand, sourceShapes, cteIndexes, methodTargets));
    }

    internal static ExecutionSubquery Create(SubqueryExpression expression, IReadOnlyList<ExecutionExpression> bindings,
        ExecutionExpression? operand)
    {
        var planning = expression.Planning ?? throw new InvalidOperationException("Subquery has no planner-owned inner strategy.");
        var shapes = (planning.Shapes as ExecutionPlanningShapeResolverAdapter)?.ExecutionShapes
            ?? throw new InvalidOperationException("Subquery lowering requires bound execution shapes.");
        var artifacts = planning.Result.ExecutionArtifacts with { GeneratedTypePrefix = expression.Identifier };
        var builder = new PhysicalToExecutionPlanBuilder(shapes, artifacts,
            planning.Schemas, planning.Options, planning.Ctes);
        var result = builder.Build(planning.Result.PhysicalArtifacts.OptimizedPhysicalPlan, expression.Identifier);
        var plan = result.ExecutionPlan ?? throw new NotSupportedException(result.UnsupportedReason);
        plan = new ExecutionIrOptimizer().Optimize(plan, planning.Options).OptimizedPlan;
        return new ExecutionSubquery(expression.Identifier, expression.Kind, plan,
            bindings, operand,
            ExecutionClrBindingFactory.FromClr(expression.ReturnType))
        {
            Comparison = expression.Comparison,
            CteBindings = ExecutionIrCollections.Freeze(shapes.Subqueries.Get(expression.Identifier)),
            CapturedCteValues = ExecutionIrCollections.Freeze(shapes.Subqueries.Get(expression.Identifier)
                .Select(static binding => binding.Capture).ToArray()),
            CapturedScriptValues = ExecutionIrCollections.Freeze(ExecutionSubqueryPlans.SelfAndDescendants(plan)
                .SelectMany(static scope => ExecutionIrAnalysis.FlattenExpressions(scope.Body))
                .Where(static value => value is ExecutionScriptParameterRead or ExecutionScriptVariableRead)
                .Distinct().ToArray())
        };
    }
}
