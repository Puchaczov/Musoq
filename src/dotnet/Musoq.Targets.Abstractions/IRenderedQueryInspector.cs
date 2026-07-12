namespace Musoq.Targets.Abstractions;

internal interface IRenderedQueryInspector
{
    ExecutionTargetId TargetId { get; }

    RenderedQueryInspection Inspect(RenderedQueryArtifact artifact);
}
