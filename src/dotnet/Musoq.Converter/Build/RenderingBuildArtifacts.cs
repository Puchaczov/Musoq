using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter.Build;

internal sealed record RenderingBuildArtifacts
{
    public RenderingBuildArtifacts(RenderedQueryArtifact artifact)
    {
        Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
        QueryMethodRenderMetadata = CSharpClrArtifactCompatibility.GetQueryMethodRenderMetadata(artifact);
    }

    public RenderingBuildArtifacts(CSharpCompilation compilation, string accessToClassPath)
        : this(CSharpClrArtifactCompatibility.CreateRenderedArtifact(compilation, accessToClassPath))
    {
    }

    public RenderingBuildArtifacts(
        CSharpCompilation compilation,
        string accessToClassPath,
        QueryMethodRenderMetadata queryMethodRenderMetadata)
        : this(CSharpClrArtifactCompatibility.CreateRenderedArtifact(compilation, accessToClassPath))
    {
        QueryMethodRenderMetadata = queryMethodRenderMetadata;
    }

    public RenderedQueryArtifact Artifact { get; init; }

    public CSharpCompilation Compilation => CSharpClrArtifactCompatibility.RequireCompilation(
        Artifact,
        "rendering artifact compilation access");

    public string AccessToClassPath => CSharpClrArtifactCompatibility.RequireAccessToClassPath(
        Artifact,
        "rendering artifact runnable type access");

    public QueryMethodRenderMetadata QueryMethodRenderMetadata { get; init; } = QueryMethodRenderMetadata.Unknown;

    public ExecutionTargetCompatibilityReport? CompatibilityReport { get; init; }

    public TargetRuntimeContract? RuntimeContract { get; init; }

    public ExecutionTargetReadinessReport? ReadinessReport { get; init; }

    public ExecutionSemanticsContract? SemanticsContract { get; init; }
}
