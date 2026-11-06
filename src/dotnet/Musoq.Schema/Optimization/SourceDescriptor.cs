using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourceDescriptor
{
    public required SourceIdentity Identity { get; init; }

    public Type? RowType { get; init; }

    public IReadOnlyList<ISchemaColumn> Columns { get; init; } = [];

    public IReadOnlyList<OptimizationDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>Provider-authored public warnings or recognized errors.</summary>
    public IReadOnlyList<Musoq.Schema.Diagnostics.DataSourceDiagnostic> ReportedDiagnostics
    {
        get => _reportedDiagnostics;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _reportedDiagnostics = Array.AsReadOnly(System.Linq.Enumerable.ToArray(value));
        }
    }

    private readonly IReadOnlyList<Musoq.Schema.Diagnostics.DataSourceDiagnostic> _reportedDiagnostics = [];

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
