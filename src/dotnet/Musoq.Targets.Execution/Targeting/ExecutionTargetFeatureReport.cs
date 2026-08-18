using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Targets.Execution;

internal enum ExecutionTargetFeatureKind
{
    ConstantKind,
    BinaryOperation,
    UnaryOperation,
    StrictCastTarget,
    Callable,
    CallableKind,
    SourceKind,
    ReadModifier,
    TypePortability,
    Container,
    DynamicValue,
    QueryRowSourceAccess
}

internal sealed record ExecutionTargetFeature
{
    public ExecutionTargetFeature(
        ExecutionTargetFeatureKind kind,
        string stableId,
        string detail)
    {
        if (string.IsNullOrWhiteSpace(stableId))
            throw new ArgumentException("Feature id cannot be empty.", nameof(stableId));
        if (string.IsNullOrWhiteSpace(detail))
            throw new ArgumentException("Feature detail cannot be empty.", nameof(detail));

        Kind = kind;
        StableId = stableId;
        Detail = detail;
    }

    public ExecutionTargetFeatureKind Kind { get; }

    public string StableId { get; }

    public string Detail { get; }
}

internal sealed record ExecutionTargetFeatureReport
{
    public ExecutionTargetFeatureReport(IEnumerable<ExecutionTargetFeature>? features)
    {
        Features = new ReadOnlyCollection<ExecutionTargetFeature>(
            (features ?? [])
            .Distinct()
            .OrderBy(static feature => feature.Kind)
            .ThenBy(static feature => feature.StableId, StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<ExecutionTargetFeature> Features { get; }

    public static ExecutionTargetFeatureReport Empty { get; } = new([]);
}
