using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Planning;

internal static class ParallelAggregateArgumentPermissions
{
    public static ParallelEligibilityCheck Check(AggregateBinding binding)
    {
        foreach (var argument in binding.SetArguments.Concat(binding.GetArguments))
        {
            var permission = ParallelFunctionPermissionPolicy.Check(argument);
            if (!permission.IsEligible)
                return permission;
        }
        return binding.FilterPredicate is null
            ? ParallelEligibilityCheck.Enabled
            : ParallelFunctionPermissionPolicy.Check(binding.FilterPredicate);
    }
}
