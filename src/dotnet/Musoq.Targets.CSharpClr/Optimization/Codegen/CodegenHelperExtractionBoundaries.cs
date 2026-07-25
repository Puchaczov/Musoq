namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

internal sealed record CodegenHelperExtractionBoundaries(
    string PhaseBoundary,
    string MutationBoundary,
    string CancellationBoundary,
    string ProgressBoundary,
    string QueryStatisticsBoundary,
    string CaptureBoundary,
    string ReturnBoundary,
    string OrderingKey);
