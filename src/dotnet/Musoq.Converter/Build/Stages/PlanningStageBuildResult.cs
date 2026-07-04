namespace Musoq.Converter.Build;

internal sealed record PlanningStageBuildResult(
    PlanningBuildArtifacts Artifacts,
    SemanticBuildArtifacts SemanticArtifacts,
    TransformPipelineContext Context);
