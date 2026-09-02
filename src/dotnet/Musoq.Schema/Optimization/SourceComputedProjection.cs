using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Optimization;

/// <summary>Operations a datasource may advertise for computed projections.</summary>
[System.Flags]
public enum SourceComputedProjectionCapabilities
{
    None = 0,
    Literals = 1 << 0,
    Columns = 1 << 1,
    Unary = 1 << 2,
    Binary = 1 << 3,
    Cast = 1 << 4,
    NullCheck = 1 << 5,
    Coalesce = 1 << 6,
    AllPortable = Literals | Columns | Unary | Binary | Cast | NullCheck | Coalesce
}

/// <summary>A requested or accepted stable computed value produced by a source row.</summary>
public sealed record SourceComputedProjection(
    string Name,
    SourceScalarExpression Expression,
    Type ResultType,
    ColumnStability Stability = ColumnStability.Stable)
{
    public bool IsStable =>
        Stability == ColumnStability.Stable && SourceScalarExpressionFacts.IsStable(Expression);
}

/// <summary>Validated accepted and residual partitions for computed projections.</summary>
public sealed record SourceComputedProjectionPartition(
    IReadOnlyList<SourceComputedProjection> Requested,
    IReadOnlyList<SourceComputedProjection> Accepted,
    IReadOnlyList<SourceComputedProjection> Residual)
{
    public static SourceComputedProjectionPartition Empty { get; } = new([], [], []);
}
