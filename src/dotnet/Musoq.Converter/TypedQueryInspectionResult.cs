using System.Collections.Generic;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter;

public sealed record TypedQueryInspectionResult(
    QueryInspectionResult? Query,
    QueryResultMode ResultMode,
    FinalResultSinkKind SelectedResultSinkKind,
    Type OutputType,
    TypedGeneratedRowsKind RowsKind,
    IReadOnlyList<string> OutputBindingDiagnostics)
{
    public string OutputTypeName { get; } = OutputType.AssemblyQualifiedName ?? OutputType.FullName ?? OutputType.Name;

    public string GeneratedCSharpCode => Query?.GeneratedCSharpCode ?? string.Empty;

    public bool HasOutputBindingDiagnostics => OutputBindingDiagnostics.Count > 0;

    public QueryResultRowPathKind RowPathKind { get; init; } = ToRowPathKind(RowsKind);

    public bool RequiresComputeTableMethod { get; init; }

    public FinalProjectionSinkRejectionKind FinalSinkRejectionKind { get; init; }

    public string? FinalSinkRejectionReason { get; init; }

    public bool HasFinalSinkRejectionDiagnostics => FinalSinkRejectionKind != FinalProjectionSinkRejectionKind.None;

    private static QueryResultRowPathKind ToRowPathKind(TypedGeneratedRowsKind rowsKind)
    {
        return rowsKind switch
        {
            TypedGeneratedRowsKind.DirectRows => QueryResultRowPathKind.DirectRows,
            TypedGeneratedRowsKind.ShardRows => QueryResultRowPathKind.ShardRows,
            TypedGeneratedRowsKind.MaterializedTableRows => QueryResultRowPathKind.MaterializedTableRows,
            _ => QueryResultRowPathKind.Unknown
        };
    }
}
