namespace Musoq.Evaluator.IR.CodeGeneration;

public readonly record struct QueryMethodRenderMetadata(
    FinalResultSinkKind FinalResultSinkKind,
    QueryResultRowPathKind RowPathKind,
    bool RequiresComputeTableMethod,
    FinalProjectionSinkRejectionKind FinalSinkRejectionKind = FinalProjectionSinkRejectionKind.None,
    string? FinalSinkRejectionReason = null)
{
    public static QueryMethodRenderMetadata Unknown { get; } = new(
        FinalResultSinkKind.TableDirect,
        QueryResultRowPathKind.Unknown,
        false);
}
