namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

internal sealed record CodegenHelperExtractionInfo(
    CodegenHelperExtractionRole Role,
    string HelperName,
    CodegenHelperExtractionCandidateKind CandidateKind,
    string PhaseBoundary,
    string MutationBoundary,
    string CancellationBoundary,
    string ProgressBoundary,
    string QueryStatisticsBoundary,
    string CaptureBoundary,
    string ReturnBoundary,
    string OrderingKey);
