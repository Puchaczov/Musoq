namespace Musoq.Targets.CSharpClr;

public sealed partial class CSharpRenderer
{
    private static QueryMethodRenderMetadata CreateTableDirectMetadata()
    {
        return new QueryMethodRenderMetadata(
            FinalResultSinkKind.TableDirect,
            QueryResultRowPathKind.Unknown,
            false);
    }

    private static QueryMethodRenderMetadata CreateMaterializedTableRowsMetadata(FinalProjectionSinkPlan? rejectedSinkPlan = null)
    {
        return new QueryMethodRenderMetadata(
            FinalResultSinkKind.TableRowsMaterialized,
            QueryResultRowPathKind.MaterializedTableRows,
            true,
            rejectedSinkPlan?.RejectionKind ?? FinalProjectionSinkRejectionKind.None,
            rejectedSinkPlan?.RejectionReason);
    }
}
