using System;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter;

public sealed record TypedQueryDiagnostics(
    Type? RunnableType,
    QueryResultMode ResultMode,
    FinalResultSinkKind SelectedResultSinkKind,
    QueryResultRowPathKind RowPathKind,
    bool RequiresComputeTableMethod,
    FinalProjectionSinkRejectionKind FinalSinkRejectionKind,
    string? FinalSinkRejectionReason,
    TypedQueryProfileMode ProfileMode)
{
    public bool IsProfiled => ProfileMode != TypedQueryProfileMode.None;

    public bool HasFinalSinkRejectionDiagnostics => FinalSinkRejectionKind != FinalProjectionSinkRejectionKind.None;

    internal static TypedQueryDiagnostics FromMetadata(
        Type? runnableType,
        QueryResultMode resultMode,
        QueryMethodRenderMetadata metadata,
        TypedQueryProfileMode profileMode = TypedQueryProfileMode.None)
    {
        return new TypedQueryDiagnostics(
            runnableType,
            resultMode,
            metadata.FinalResultSinkKind,
            metadata.RowPathKind,
            metadata.RequiresComputeTableMethod,
            metadata.FinalSinkRejectionKind,
            metadata.FinalSinkRejectionReason,
            profileMode);
    }
}
