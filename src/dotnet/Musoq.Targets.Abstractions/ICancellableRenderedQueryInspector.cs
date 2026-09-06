using System.Threading;

namespace Musoq.Targets.Abstractions;

/// <summary>
/// Optional cancellation-aware inspection contract. The legacy inspector contract remains unchanged.
/// </summary>
internal interface ICancellableRenderedQueryInspector
{
    RenderedQueryInspection Inspect(RenderedQueryArtifact artifact, CancellationToken cancellationToken);
}
