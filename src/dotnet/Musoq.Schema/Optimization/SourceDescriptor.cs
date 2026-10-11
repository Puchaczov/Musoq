using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourceDescriptor
{
    public required SourceIdentity Identity { get; init; }

    public Type? RowType { get; init; }

    public IReadOnlyList<ISchemaColumn> Columns { get; init; } = [];

    public IReadOnlyList<OptimizationDiagnostic> Diagnostics { get; init; } = [];

    public IReadOnlyList<SourceContractDiagnostic> ContractDiagnostics { get; init; } = [];

    public SourceTransferCapabilities TransferCapabilities { get; init; }

    /// <summary>Gets permissions explicitly granted for source calls and supplied row processing.</summary>
    public SourceExecutionPermissions ExecutionPermissions { get; init; } = SourceExecutionPermissions.None;

    /// <summary>Optional typed predicates the source explicitly agrees to evaluate.</summary>
    public SourcePredicateCapabilities PredicateCapabilities { get; init; } = SourcePredicateCapabilities.None;

    public static SourceDescriptor Empty(SourceIdentity identity)
    {
        return new SourceDescriptor { Identity = identity };
    }
}
