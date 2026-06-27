using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static HashSet<string> CreateInPlaceSortableSortedPartitionSignatures(
        IReadOnlyList<WindowRegistrationBuildResult> registrationResults)
    {
        var usages = new Dictionary<string, WindowPartitionSortUsage>(StringComparer.Ordinal);

        foreach (var registrationResult in registrationResults)
        {
            if (!registrationResult.Supported || registrationResult.Registration == null)
                continue;

            var registration = registrationResult.Registration;
            var partitionSignature = WindowRegistrationLoweringHelpers.CreateWindowPartitionListSignature(registration);
            if (!usages.TryGetValue(partitionSignature, out var usage))
            {
                usage = new WindowPartitionSortUsage();
                usages.Add(partitionSignature, usage);
            }

            var sortedSignature = WindowRegistrationLoweringHelpers.CreateWindowSortedPartitionListSignature(registration);
            if (sortedSignature == null)
                usage.HasUnsortedConsumer = true;
            else
                usage.SortedSignatures.Add(sortedSignature);
        }

        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var usage in usages.Values)
        {
            if (usage is { HasUnsortedConsumer: false, SortedSignatures.Count: 1 })
                result.Add(usage.SortedSignatures.Single());
        }

        return result;
    }

    private static HashSet<string> CreateSingleUsePartitionKeySignatures(
        IReadOnlyList<WindowRegistrationBuildResult> registrationResults)
    {
        var usages = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var registrationResult in registrationResults)
        {
            if (!registrationResult.Supported || registrationResult.Registration == null)
                continue;

            var signature = WindowRegistrationLoweringHelpers.CreateWindowPartitionSignature(registrationResult.Registration);
            if (signature == null)
                continue;

            usages[signature] = usages.TryGetValue(signature, out var count) ? count + 1 : 1;
        }

        return usages
            .Where(static usage => usage.Value == 1)
            .Select(static usage => usage.Key)
            .ToHashSet(StringComparer.Ordinal);
    }
}
