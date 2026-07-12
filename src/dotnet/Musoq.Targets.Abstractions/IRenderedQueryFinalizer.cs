namespace Musoq.Targets.Abstractions;

internal interface IRenderedQueryFinalizer
{
    ExecutionTargetId TargetId { get; }

    TargetFinalizationResult Finalize(RenderedQueryArtifact artifact, TargetFinalizationOptions options);
}
