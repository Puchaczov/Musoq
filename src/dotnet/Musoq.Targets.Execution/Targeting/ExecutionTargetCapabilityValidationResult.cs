using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution;

internal sealed record ExecutionTargetCapabilityValidationResult
{
    public ExecutionTargetCapabilityValidationResult(
        IReadOnlyList<ExecutionTargetRequirement>? unsupportedRequirements,
        IReadOnlyList<ExecutionOperationId>? unsupportedOperations,
        IReadOnlyList<int>? unsupportedSemanticsVersions,
        IReadOnlyList<string>? unsupportedSemanticsFingerprints,
        IReadOnlyList<ExecutionTargetFeature>? unsupportedFeatures)
    {
        UnsupportedRequirements = Freeze(unsupportedRequirements);
        UnsupportedOperations = Freeze(unsupportedOperations);
        UnsupportedSemanticsVersions = Freeze(unsupportedSemanticsVersions);
        UnsupportedSemanticsFingerprints = Freeze(unsupportedSemanticsFingerprints);
        UnsupportedFeatures = Freeze(unsupportedFeatures);
    }

    public IReadOnlyList<ExecutionTargetRequirement> UnsupportedRequirements { get; }

    public IReadOnlyList<ExecutionOperationId> UnsupportedOperations { get; }

    public IReadOnlyList<int> UnsupportedSemanticsVersions { get; }

    public IReadOnlyList<string> UnsupportedSemanticsFingerprints { get; }

    public IReadOnlyList<ExecutionTargetFeature> UnsupportedFeatures { get; }

    public bool IsSupported => UnsupportedRequirements.Count == 0 &&
                               UnsupportedOperations.Count == 0 &&
                               UnsupportedSemanticsVersions.Count == 0 &&
                               UnsupportedSemanticsFingerprints.Count == 0 &&
                               UnsupportedFeatures.Count == 0;

    public string FormatUnsupportedRequirements(string targetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);

        if (IsSupported)
            return $"Execution target '{targetName}' supports all reported requirements.";

        var formatted = UnsupportedRequirements
            .Select(static requirement => $"{requirement.Kind}: {requirement.Detail}")
            .Concat(UnsupportedOperations.Select(static operation => $"operation: {operation}"))
            .Concat(UnsupportedSemanticsVersions.Select(static version => $"semantics-version: {version}"))
            .Concat(UnsupportedSemanticsFingerprints.Select(static fingerprint => $"semantics-fingerprint: {fingerprint}"))
            .Concat(UnsupportedFeatures.Select(static feature => $"feature: {feature.StableId}"));

        return $"Execution target '{targetName}' does not support: {string.Join("; ", formatted)}";
    }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}
