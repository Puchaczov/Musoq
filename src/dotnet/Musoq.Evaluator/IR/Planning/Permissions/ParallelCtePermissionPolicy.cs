using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Planning;

internal static class ParallelCtePermissionPolicy
{
    public static ParallelEligibilityCheck CheckLevel(PhysicalCteNode node, IReadOnlyCollection<string> names,
        ParallelSourcePermissionPolicy sourcePermissions)
    {
        foreach (var definition in node.Definitions.Where(definition => names.Contains(definition.Name)))
        {
            var access = sourcePermissions.Check(definition.Plan, Musoq.Schema.Optimization.SourceExecutionPermissions.ConcurrentAccess |
                Musoq.Schema.Optimization.SourceExecutionPermissions.ConcurrentRowProcessing);
            if (!access.IsEligible)
                return access;
            var calls = Check(definition.Plan);
            if (!calls.IsEligible)
                return calls;
        }
        return ParallelEligibilityCheck.Enabled;
    }

    public static ParallelEligibilityCheck Check(PhysicalNode node)
    {
        var own = CheckNode(node);
        if (!own.IsEligible)
            return own;
        foreach (var child in node.Children)
        {
            var permission = Check(child);
            if (!permission.IsEligible)
                return permission;
        }
        return ParallelEligibilityCheck.Enabled;
    }

    private static ParallelEligibilityCheck CheckNode(PhysicalNode node)
        => node switch
        {
            PhysicalProjectNode project => CheckExpressions(project.Fields.Select(static field => field.Expression)),
            PhysicalFilterNode filter => ParallelFunctionPermissionPolicy.Check(filter.Predicate),
            PhysicalSortNode sort => CheckExpressions(sort.Keys.Select(static key => key.Expression)),
            PhysicalTopNNode top => CheckExpressions(top.Keys.Select(static key => key.Expression)),
            PhysicalTopOffsetNode top => CheckExpressions(top.Keys.Select(static key => key.Expression)),
            PhysicalSchemaScanNode scan => CheckExpressions(scan.Arguments.Concat(scan.PushedPredicates)),
            PhysicalValuesScanNode values => CheckExpressions(values.Rows.SelectMany(static row => row.Fields).Select(static field => field.Value)),
            PhysicalHavingFilterNode having => ParallelFunctionPermissionPolicy.Check(having.Predicate),
            PhysicalHashJoinNode join => CheckJoin(join),
            PhysicalSingleKeyAggregateNode aggregate => CheckAggregate([aggregate.GroupKey], aggregate.Bindings),
            PhysicalValueTupleAggregateNode aggregate => CheckAggregate(aggregate.GroupKeys, aggregate.Bindings),
            PhysicalAggregateOnlyNode aggregate => CheckAggregate([], aggregate.Bindings),
            PhysicalWindowNode window => CheckWindows(window.Registrations),
            PhysicalCteRefNode or PhysicalTakeNode or PhysicalSkipNode or PhysicalSetOperationNode or
                PhysicalMultiStatementNode or PhysicalCteNode => ParallelEligibilityCheck.Enabled,
            _ => ParallelEligibilityCheck.Skipped($"Operation '{node.GetType().Name}' has unresolved function scheduling permissions.")
        };

    private static ParallelEligibilityCheck CheckJoin(PhysicalHashJoinNode join)
    {
        var expressions = join.BuildKeys.Concat(join.ProbeKeys);
        if (join.Residual is not null)
            expressions = expressions.Append(join.Residual);
        return CheckExpressions(expressions);
    }

    private static ParallelEligibilityCheck CheckAggregate(IEnumerable<IrExpression> keys, AggregateBinding[] bindings)
    {
        const FunctionExecutionPermissions required = FunctionExecutionPermissions.ConcurrentInvocations |
            FunctionExecutionPermissions.IndependentGroups;
        if (bindings.Any(binding => binding.Kernel is null || (binding.Kernel.ExecutionPermissions & required) != required))
            return ParallelEligibilityCheck.Skipped("Aggregate declarations do not permit concurrent independent groups in CTE branches.");
        return CheckExpressions(keys.Concat(bindings.SelectMany(static binding => binding.SetArguments.Concat(binding.GetArguments)))
            .Concat(bindings.Where(static binding => binding.FilterPredicate is not null).Select(static binding => binding.FilterPredicate!)));
    }

    private static ParallelEligibilityCheck CheckWindows(WindowRegistration[] registrations)
    {
        const FunctionExecutionPermissions required = FunctionExecutionPermissions.ConcurrentInvocations |
            FunctionExecutionPermissions.IndependentPartitions;
        foreach (var window in registrations)
        {
            if (window.Function is null || !SqlFunctionDeclarations.Read(window.Function).Any(declaration =>
                    declaration.Kind == SqlFunctionKind.Window && (declaration.ExecutionPermissions & required) == required))
                return ParallelEligibilityCheck.Skipped($"Window '{window.FunctionName}' does not permit concurrent independent partitions.");
            var expressions = window.ValueArguments.Concat(window.PartitionKeys)
                .Concat(window.OrderKeys.Select(static key => key.Expression));
            if (window.FilterPredicate is not null)
                expressions = expressions.Append(window.FilterPredicate);
            var permission = CheckExpressions(expressions);
            if (!permission.IsEligible)
                return permission;
        }
        return ParallelEligibilityCheck.Enabled;
    }

    private static ParallelEligibilityCheck CheckExpressions(IEnumerable<IrExpression> expressions)
    {
        foreach (var expression in expressions)
        {
            var permission = ParallelFunctionPermissionPolicy.Check(expression);
            if (!permission.IsEligible)
                return permission;
        }
        return ParallelEligibilityCheck.Enabled;
    }
}
