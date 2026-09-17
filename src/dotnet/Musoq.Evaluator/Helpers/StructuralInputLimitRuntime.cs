using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Helpers;

/// <summary>Small allocation-free terminal checks used by generated structural preparation.</summary>
public static class StructuralInputLimitRuntime
{
    /// <summary>Throws the first deterministic resource-limit failure for measured values.</summary>
    public static void ThrowIfExceeded(
        string origin,
        string path,
        StructuralInputMetrics metrics,
        int maxDepth,
        long maxNodes,
        long maxStringBytes,
        CancellationToken cancellationToken,
        string? sourceContextId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        if (metrics.MaxDepth > maxDepth)
            throw new StructuralInputLimitExceededException(
                origin,
                path,
                StructuralInputLimitKind.Depth,
                maxDepth,
                metrics.MaxDepth,
                sourceContextId);

        if (metrics.NodeCount > maxNodes)
            throw new StructuralInputLimitExceededException(
                origin,
                path,
                StructuralInputLimitKind.Nodes,
                maxNodes,
                metrics.NodeCount,
                sourceContextId);

        if (metrics.StringBytes > maxStringBytes)
            throw new StructuralInputLimitExceededException(
                origin,
                path,
                StructuralInputLimitKind.StringBytes,
                maxStringBytes,
                metrics.StringBytes,
                sourceContextId);
    }
}
