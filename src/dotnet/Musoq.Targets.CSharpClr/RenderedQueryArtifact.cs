using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Targets.CSharpClr;

internal sealed record CSharpRenderedQueryArtifact(
    CSharpCompilation Compilation,
    string AccessToClassPath,
    QueryMethodRenderMetadata QueryMethodRenderMetadata,
    OptimizationTrace? OptimizationTrace) : RenderedQueryArtifact(ExecutionTargetIds.CSharpClr)
{
    public CSharpRenderedQueryArtifact(
        CSharpCompilation compilation,
        string accessToClassPath)
        : this(
            compilation,
            accessToClassPath,
            QueryMethodRenderMetadata.Unknown,
            null)
    {
    }
}
