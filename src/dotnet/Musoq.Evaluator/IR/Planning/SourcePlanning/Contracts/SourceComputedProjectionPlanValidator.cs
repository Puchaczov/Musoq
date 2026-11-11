using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static class SourceComputedProjectionPlanValidator
{
    public static void Validate(SourcePlanRequest request, SourcePlanResult result, SourceDescriptor descriptor)
    {
        if (!SourceComputedProjectionNegotiator.TryPartition(request.RequestedComputedProjections,
                result.AcceptedComputedProjections, descriptor.ComputedProjectionCapabilities,
                out var partition, out var diagnostic))
            throw new SourcePlanContractException(diagnostic, TextSpan.Empty);

        RequireSame(partition.Residual, result.ResidualComputedProjections, "residual");
        RequireSame(partition.Accepted, result.ExecutionPlan.AcceptedComputedProjections, "execution");
    }

    private static void RequireSame(IReadOnlyList<SourceComputedProjection> expected,
        IReadOnlyList<SourceComputedProjection> actual, string operation)
    {
        var byName = expected.ToDictionary(item => item.Name, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (expected.Count != actual.Count || actual.Any(item => item is null ||
                !seen.Add(item.Name) || !byName.TryGetValue(item.Name, out var original) ||
                !SourceComputedProjectionFacts.CanProviderEvaluate(item, SourceComputedProjectionCapabilities.AllPortable) ||
                item.ResultType != original.ResultType || item.Stability != original.Stability ||
                SourceScalarExpressionFingerprint.Compute(item.Expression) != SourceScalarExpressionFingerprint.Compute(original.Expression)))
            throw new SourcePlanContractException($"Computed projection {operation} partition does not match the request and acceptance.", TextSpan.Empty);
    }
}
