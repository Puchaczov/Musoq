namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatForEachName(ExecutionExpression source) =>
        ExecutionRowStreams.IsScalar(source) ? "ScalarForEach" :
        ExecutionRowStreams.IsChunked(source) ? "ChunkedForEach" : "ForEach";

    private static string FormatForEachWithOrdinalityName(ExecutionExpression source) =>
        ExecutionRowStreams.IsScalar(source) ? "ScalarForEachWithOrdinality" :
        ExecutionRowStreams.IsChunked(source) ? "ChunkedForEachWithOrdinality" : "ForEachWithOrdinality";

    private static string FormatMaterializeName(ExecutionExpression source) =>
        ExecutionRowStreams.IsChunked(source) ? "MaterializeChunked" : "Materialize";

    private static string FormatMaterializeFilteredName(ExecutionExpression source) =>
        ExecutionRowStreams.IsChunked(source) ? "MaterializeFilteredChunked" : "MaterializeFiltered";

    private static string FormatMaterializeExpandoName(ExecutionExpression source) =>
        ExecutionRowStreams.IsChunked(source) ? "MaterializeChunkedExpando" : "MaterializeExpando";
}
