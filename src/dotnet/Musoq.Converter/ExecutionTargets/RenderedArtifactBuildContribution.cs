using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Converter.Build;

internal sealed record RenderedArtifactBuildContribution(
    QueryMethodRenderMetadata QueryMethodRenderMetadata,
    OptimizationTrace? OptimizationTrace,
    string? GeneratedCodeSha256 = null)
{
    public static RenderedArtifactBuildContribution Empty { get; } = new(
        QueryMethodRenderMetadata.Unknown,
        null,
        null);
}
