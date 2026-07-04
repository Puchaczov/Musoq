namespace Musoq.Converter.Build;

internal sealed record ExecutionStageBuildResult(
    ExecutionBuildArtifacts Artifacts,
    TransformPipelineContext Context);
