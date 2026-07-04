namespace Musoq.Converter.Build;

internal sealed record RenderingStageBuildResult(
    RenderingBuildArtifacts Artifacts,
    TransformPipelineContext Context);
