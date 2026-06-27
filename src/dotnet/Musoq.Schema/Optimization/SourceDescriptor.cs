using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourceDescriptor
{
    public required SourceIdentity Identity { get; init; }

    public Type? RowType { get; init; }

    public IReadOnlyList<ISchemaColumn> Columns { get; init; } = [];

    public IReadOnlyList<OptimizationDiagnostic> Diagnostics { get; init; } = [];

    public IReadOnlyList<SourceContractDiagnostic> ContractDiagnostics { get; init; } = [];

    public static SourceDescriptor Empty(SourceIdentity identity)
    {
        return new SourceDescriptor { Identity = identity };
    }
}
