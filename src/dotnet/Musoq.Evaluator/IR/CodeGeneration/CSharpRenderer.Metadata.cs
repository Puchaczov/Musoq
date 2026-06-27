namespace Musoq.Evaluator.IR.CodeGeneration;

public sealed partial class CSharpRenderer
{
    private static QueryMethodRenderMetadata CreateTableDirectMetadata()
    {
        return new QueryMethodRenderMetadata(
            FinalResultSinkKind.TableDirect,
            QueryResultRowPathKind.Unknown,
            false);
    }

    private static QueryMethodRenderMetadata CreateTableFallbackMetadata(FinalProjectionSinkPlan? rejectedSinkPlan = null)
    {
        return new QueryMethodRenderMetadata(
            FinalResultSinkKind.TableRowsMaterialized,
            QueryResultRowPathKind.TableFallback,
            true,
            rejectedSinkPlan?.RejectionKind ?? FinalProjectionSinkRejectionKind.None,
            rejectedSinkPlan?.RejectionReason);
    }
}
