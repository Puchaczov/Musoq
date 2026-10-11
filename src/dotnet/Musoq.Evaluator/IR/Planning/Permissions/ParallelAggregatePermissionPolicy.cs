using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Planning;

internal static class ParallelAggregatePermissionPolicy
{
    public static ParallelEligibilityCheck Check(AggregateBinding[] bindings)
    {
        if (bindings.Length == 0)
            return ParallelEligibilityCheck.Skipped("No aggregate set operations are present for mergeable parallel aggregation.");
        if (bindings.Any(static binding => binding.ParentDepth > 0))
            return ParallelEligibilityCheck.Skipped("Aggregate binding requires parent group links, which cannot be merged in the parallel aggregate loop.");
        if (bindings.Any(static binding => binding.Kernel == null))
            return ParallelEligibilityCheck.Skipped("At least one aggregate binding has no typed aggregate kernel for parallel merging.");
        if (bindings.Any(static binding => binding.Kernel is { SupportsMerge: false }))
            return ParallelEligibilityCheck.Skipped("At least one aggregate binding is not mergeable.");

        const FunctionExecutionPermissions required = FunctionExecutionPermissions.ConcurrentInvocations |
            FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge;
        if (bindings.Any(binding => (binding.Kernel!.ExecutionPermissions & required) != required))
            return ParallelEligibilityCheck.Skipped("Aggregate declarations do not explicitly permit concurrent independent groups with equivalent partial merging.");
        foreach (var binding in bindings)
        {
            var permission = ParallelAggregateArgumentPermissions.Check(binding);
            if (!permission.IsEligible)
                return permission;
        }
        return ParallelEligibilityCheck.Enabled;
    }
}
